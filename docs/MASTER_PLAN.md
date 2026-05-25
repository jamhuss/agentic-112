# Master Plan — Agentic-112

## Vision

Ett ärendehanteringssystem för nödsituationer med **tvåstegs AI-pipeline**: klassificering + trovärdighetsbedömning. Alla ärenden — manuella och AI-skapade — genomgår trovärdighetskontroll. Varje steg loggas och visualiseras transparent i frontend.

---

## Nuläge (exakt kodtillstånd)

> Punkter markerade med **Saknar/Behöver** beskriver vad som saknas i koden idag och åtgärdas i respektive fas.

### Backend (`/server`)

- **Domain/Entities/Incident.cs** — Grundmodell med Id, Description, Services, Priority, Status (`"ongoing"`), CreatedBy, Confidence, Credibility, NeedsHumanReview, CreatedAt. **Saknar:** `List<PipelineStep> Steps` (Fas 1), status default bör vara `"pending_review"` (Fas 1)
- **Domain/Models/IncidentAnalysis.cs** — Record med Services, Priority, Confidence, Credibility, NeedsHumanReview, Reasoning. **Behöver:** ta bort Credibility + NeedsHumanReview (Fas 1), göra Confidence nullable (Fas 1)
- **Domain/Models/CredibilityAssessment.cs** — Ny record: Credibility, NeedsHumanReview, Reasoning ✅
- **Domain/Models/PipelineStep.cs** — **Finns inte, skapas i Fas 1**
- **Domain/DTOS/** — CreateManualRequest, CreateAiRequest, UpdateIncidentRequest
- **Domain/Contstants/IncidentConstants.cs** — Services, Priorities, CredibilityLevels. **Saknar:** Statuses-lista (Fas 1)
- **Application/Interfaces/IAiGateway.cs** — `AnalyzeAsync(string description)` ✅
- **Application/Interfaces/ICredibilityGateway.cs** — `AssessAsync(description, services, priority, createdBy)` ✅
- **Application/Interfaces/IIncidentRepository.cs** — SaveAsync, GetAllAsync, GetByIdAsync, UpdateAsync ✅
- **Application/Services/IncidentService.cs** — CreateManualAsync, CreateFromAiAsync. **Behöver:** tvåstegsflöde med ICredibilityGateway (Fas 2)
- **Infrastructure/AI/AiGateway.cs** — Fake-implementation. **Bugg:** returnerar `"Ambulans"` (ska vara `"ambulance"`), `"High"` (ska vara `"high"`), innehåller fortfarande Credibility/NeedsHumanReview-fält (Fas 3)
- **Infrastructure/AI/CredibilityGateway.cs** — **Finns inte, skapas i Fas 3**
- **Infrastructure/Persistence/InMemoryIncidentRepository.cs** — Minnesbaserad lagring. **Bugg:** inte trådsäker, `List<Incident>` utan lås (Fas 1)
- **Api/Controllers/IncidentsController.cs** — POST manual, POST ai, GET all, PATCH {id}. **Bugg:** PATCH validerar inte status mot `IncidentConstants.Statuses` (Fas 2)
- **Program.cs** — DI-registrering. **Buggar:** `AddEndpointsApiExplorer()` anropas dubbelt (Fas 1), saknar `ICredibilityGateway`-registrering (Fas 2), saknar CORS (Fas 1)

### Frontend (`/client`)

- React + TypeScript + Vite
- **Tabellvy** med kolumner: Beskrivning, Tjänster, Prioritet, Status, Skapad av, Datum, Redigera
- **CreateIncidentModal** — Manuellt + Agentic-läge (agentic visar bara beskrivningsfält)
- **EditIncidentModal** — Redigera alla fält + status (bara `ongoing`/`closed`). **Bugg:** kräver `services.length > 0` för submit, men det kan saknas vid redigering (Fas 4)
- **labels.ts** — Svenska mappningar. **Saknar:** `pending_review`, `flagged`, `rejected` (Fas 4)
- **api.ts** — fetchIncidents, createManualIncident, createAgenticIncident, updateIncident. **Bugg:** `createAgenticIncident` skickar `CreateManualRequest`-typ till `/ai` som förväntar sig bara `{ description }` (Fas 4)
- **types.ts** — Incident, CreateManualRequest, UpdateIncidentRequest. **Saknar:** `PipelineStep` interface, `steps` på Incident, separat `CreateAiRequest`-typ (Fas 4)
- Vite proxy `/api` → `http://localhost:5236`

---

## Fas 1 — Omstrukturering av modeller + buggfixar

**Mål:** Separera klassificering från trovärdighetskontroll. Introducera PipelineStep för spårbarhet. Fixa kända buggar.

### Uppgifter

- [ ] **Domain/Models/IncidentAnalysis.cs** — Ta bort `Credibility`, `NeedsHumanReview`. Gör Confidence nullable (`double?`). Behåll: `Services`, `Priority`, `Confidence`, `Reasoning`
- [ ] **Domain/Models/PipelineStep.cs** — NY modell:
  ```csharp
  public record PipelineStep(
      string Name,       // "classification" eller "credibility_check"
      string Result,     // strukturerad sammanfattning, t.ex. "services: [police], priority: high"
      string Reasoning,  // AI:ns motivering i fritext
      DateTime Timestamp
  );
  ```
  > **OBS:** `Result` är fritext-sammanfattning av stegets output. Hålls enkelt nu, kan bli strukturerat objekt i Fas 5 om det behövs.
- [ ] **Domain/Entities/Incident.cs** — Lägg till `List<PipelineStep> Steps = new()`, ändra `Status` default till `"pending_review"`
- [ ] **Domain/Contstants/IncidentConstants.cs** — Lägg till:
  ```csharp
  public static readonly List<string> Statuses = new()
  {
      "pending_review", "ongoing", "flagged", "rejected", "closed"
  };
  ```
- [ ] **Infrastructure/Persistence/InMemoryIncidentRepository.cs** — Gör trådsäker: lägg till `lock` runt alla operationer på `_storage`
- [ ] **Program.cs** — Ta bort dubbletten av `AddEndpointsApiExplorer()`
- [ ] **Program.cs** — Lägg till CORS-konfiguration (`AllowAnyOrigin` i dev, explicit origin i prod)

### Separation av ansvar

| Modell | Fråga den besvarar |
|--------|---------------------|
| `IncidentAnalysis` | VAD hände? (tjänster, prioritet, confidence) |
| `CredibilityAssessment` | ÄR DET TROVÄRDIGT? (credibility, needsHumanReview, reasoning) |
| `PipelineStep` | LOGG — vad beslutade systemet och varför? |

---

## Fas 2 — Tvåstegsflöde i IncidentService

**Mål:** Alla ärenden passerar trovärdighetskontroll efter skapande.

### Nytt flöde

```
CreateManualAsync(description, services, priority):
  1. Skapa incident med status "pending_review"
  2. SaveAsync(incident)
  3. Kör ICredibilityGateway.AssessAsync(description, services, priority, "User")
  4. Lägg till PipelineStep("credibility_check", result, reasoning, now)
  5. Bestäm status enligt statuslogik
  6. UpdateAsync(incident)
  7. Returnera incident

CreateFromAiAsync(description):
  1. Kör IAiGateway.AnalyzeAsync(description)
  2. Skapa incident med status "pending_review", confidence från analysen
  3. Lägg till PipelineStep("classification", result, reasoning, now)
  4. SaveAsync(incident)
  5. Kör ICredibilityGateway.AssessAsync(description, services, priority, "AI")
  6. Lägg till PipelineStep("credibility_check", result, reasoning, now)
  7. Bestäm status enligt statuslogik
  8. UpdateAsync(incident)
  9. Returnera incident
```

### Statuslogik efter trovärdighetskontroll

| Credibility | Confidence | → Status | → NeedsHumanReview |
|-------------|-----------|----------|---------------------|
| `high` | any | `ongoing` | `false` |
| `medium` | ≥ 0.6 | `ongoing` | `false` |
| `medium` | < 0.6 | `flagged` | `true` |
| `low` | any | `flagged` | `true` |

> **Manuella ärenden:** Saknar `Confidence` (null). Behandla null som 1.0 i statuslogiken — manuella ärenden bedöms bara på credibility.

### Felhantering i tvåstegsflödet

Om `ICredibilityGateway.AssessAsync()` kastar exception:
1. Fånga exception i `IncidentService`
2. Sätt `Credibility = null`, `NeedsHumanReview = true`, `Status = "flagged"`
3. Lägg till PipelineStep("credibility_check", "ERROR", "Trovärdighetskontroll misslyckades: {exception.Message}", now)
4. `UpdateAsync(incident)` — ärendet sparas ändå, men flaggas för manuell granskning
5. Logga exception

### Uppgifter

- [ ] **Application/Services/IncidentService.cs** — Injicera `ICredibilityGateway`, implementera tvåstegsflöde enligt ovan, inkl. felhantering
- [ ] **Program.cs** — Registrera `ICredibilityGateway` i DI
- [ ] **Api/Controllers/IncidentsController.cs** — PATCH: validera `request.Status` mot `IncidentConstants.Statuses` innan uppdatering, returnera `400 Bad Request` vid ogiltigt värde
- [ ] **Api/Controllers/IncidentsController.cs** — NY endpoint: `GET /api/constants` — returnerar tillgängliga services, priorities, statuses, credibilityLevels (så frontend inte hårdkodar)
- [ ] **Enhetstester** — Testa statuslogiken:
  - high + null confidence → ongoing
  - medium + 0.7 → ongoing
  - medium + 0.4 → flagged
  - low + any → flagged
  - credibility gateway throws → flagged + needsHumanReview

---

## Fas 3 — Fake-implementationer (MVP)

**Mål:** Hela flödet fungerar end-to-end med simulerade AI-svar.

### Uppgifter

- [ ] **Infrastructure/AI/AiGateway.cs** — Uppdatera: returnera `IncidentAnalysis` UTAN credibility-fält. **Fixa värden:** använd `"ambulance"` (inte `"Ambulans"`), `"high"` (inte `"High"`) — måste matcha `IncidentConstants`
- [ ] **Infrastructure/AI/CredibilityGateway.cs** — NY implementation av `ICredibilityGateway`:
  - Beskrivning innehåller "test" / "hej" / "asdf" → `low`, `needsHumanReview: true`, `"Ser ut som ett test"`
  - Beskrivning < 10 tecken → `low`, `needsHumanReview: true`, `"För kort beskrivning för att bedöma"`
  - Annars → `high`, `needsHumanReview: false`, `"Realistisk och trovärdig beskrivning"`
  > **OBS:** Fake-implementationen använder bara `description` för att avgöra. Parametrarna `services`, `priority`, `createdBy` skickas med men används först i Fas 5 (riktig AI).
- [ ] Verifiera: skapa manuellt ärende → får credibility-steg
- [ ] Verifiera: skapa AI-ärende → får classification + credibility-steg
- [ ] Verifiera: "test"-ärende → status `flagged`
- [ ] Verifiera: vanligt ärende → status `ongoing`

---

## Fas 4 — Frontend-ombyggnad

**Mål:** Byt från tabellvy till kortvy med pipeline-visualisering.

### Nya/ändrade filer

- [ ] **types.ts** — Lägg till `PipelineStep` interface, lägg till `steps: PipelineStep[]` på `Incident`. Lägg till separat `CreateAiRequest` typ (bara `{ description }`)
- [ ] **labels.ts** — Uppdatera `STATUS_LABELS`:
  ```typescript
  export const STATUS_LABELS: Record<string, string> = {
    pending_review: "Granskas",
    ongoing: "Pågående",
    flagged: "Flaggad",
    rejected: "Avvisad",
    closed: "Avslutat",
  };
  ```
- [ ] **api.ts** — Fixa `createAgenticIncident`: använd `CreateAiRequest` (bara description), inte `CreateManualRequest`. Lägg till `fetchConstants()` som hämtar `GET /api/constants`
- [ ] **components/PipelineSteps.tsx** — NY komponent:
  - Renderar `steps[]` med ✅/❌ ikoner
  - Visar reasoning för varje steg
  - Manuella ärenden: visar "Manuellt skapat" istället för classification-steg
- [ ] **components/IncidentCard.tsx** — NY komponent (ersätter tabellrad):
  - Beskrivning + metadata (tjänster, prioritet, datum, skapad av)
  - Status-badge med färgkodning
  - Pipeline-steg (PipelineSteps)
  - Edit-knapp
  - Flaggade ärenden: "Godkänn" / "Avvisa"-knappar (anropar PATCH med `status: "ongoing"` resp. `status: "rejected"`)
- [ ] **components/IncidentList.tsx** — Ändra till att rendera `IncidentCard` istället för tabell
- [ ] **components/EditIncidentModal.tsx** — Uppdatera status-dropdown med alla fem statusar. Fixa: gör services-validering valfri (tillåt submit utan services vid redigering)
- [ ] **App.css** — Nya styles:
  - Kortstyles (bakgrund, skugga, padding)
  - Pipeline-steg (ikon + text)
  - Nya status-badges: `pending_review` (grå), `flagged` (orange), `rejected` (röd), `closed` (grön)
  - Operatörsknappar (godkänn/avvisa)

### Kortlayout (wireframe)

```
┌──────────────────────────────────────────────────┐
│ 🔴 Brand i lagerhus på Storgatan 5               │
│ Prioritet: Kritisk  │  Tjänster: Räddningstjänst │
│ Status: Pågående    │  Skapad av: AI             │
│                                                  │
│ ── Pipeline ──────────────────────────────────── │
│ ✅ Klassificering  "Brandrelaterad, hög fara"     │
│ ✅ Trovärdighet    "Hög — realistisk beskrivning" │
│                                           [Edit] │
└──────────────────────────────────────────────────┘
```

---

## Fas 5 — Riktig AI-integration

**Mål:** Byt ut fake-implementationer mot riktiga LLM-anrop.

### Teknologival

| Komponent | Val |
|-----------|-----|
| Abstraktion | `Microsoft.Extensions.AI` (`IChatClient`) |
| LLM-provider | Azure OpenAI (GPT-4o) |
| Output-format | Structured Outputs (JSON Schema) |
| Konfiguration | `appsettings.json` + User Secrets |

### Ny mappstruktur under `Infrastructure/AI/`

```
Infrastructure/AI/
├── AiGateway.cs                  ← Implementerar IAiGateway med IChatClient
├── CredibilityGateway.cs         ← Implementerar ICredibilityGateway med IChatClient
├── Prompts/
│   ├── ClassificationPrompt.cs   ← System prompt + JSON schema för klassificering
│   └── CredibilityPrompt.cs      ← System prompt + JSON schema för trovärdighet
├── Parsing/
│   └── AiResponseValidator.cs    ← Validerar att AI-svar matchar IncidentConstants
└── Configuration/
    └── AiOptions.cs              ← Endpoint, modell, temperature, max retries
```

### Uppgifter

- [ ] **server.csproj** — Lägg till `Microsoft.Extensions.AI.OpenAI` NuGet-paket
- [ ] **Infrastructure/AI/Configuration/AiOptions.cs** — NY:
  ```csharp
  public class AiOptions
  {
      public string Endpoint { get; set; } = "";
      public string Model { get; set; } = "gpt-4o";
      public double Temperature { get; set; } = 0.2;
      public int MaxRetries { get; set; } = 2;
  }
  ```
- [ ] **appsettings.json** — Lägg till AI-sektion (utan hemligheter)
- [ ] **User Secrets** — `dotnet user-secrets set "AI:ApiKey" "..."`
- [ ] **Infrastructure/AI/Prompts/ClassificationPrompt.cs** — NY:
  - System prompt: "Du är en SOS Alarm-operatör..."
  - JSON schema som tvingar: services (enum), priority (enum), confidence (0-1), reasoning
- [ ] **Infrastructure/AI/Prompts/CredibilityPrompt.cs** — NY:
  - System prompt: "Bedöm trovärdigheten..."
  - JSON schema: credibility (enum), needsHumanReview (bool), reasoning
- [ ] **Infrastructure/AI/Parsing/AiResponseValidator.cs** — NY:
  - Validera services mot `IncidentConstants.Services`
  - Validera priority mot `IncidentConstants.Priorities`
  - Validera credibility mot `IncidentConstants.CredibilityLevels`
  - Om ogiltigt → retry 1x → fallback till `flagged` + `needsHumanReview = true`
- [ ] **Infrastructure/AI/AiGateway.cs** — Omskriv med `IChatClient`, prompt, structured output
- [ ] **Infrastructure/AI/CredibilityGateway.cs** — Omskriv med `IChatClient`, prompt, structured output
- [ ] **Program.cs** — Registrera `IChatClient`, bind `AiOptions` från config

---

## Fas 6 — Produktion och kvalitet

**Mål:** Produktionsredo kod med felhantering, retry och observabilitet.

### Uppgifter

- [ ] **Retry-logik** — Implementera i AiGateway och CredibilityGateway:
  - Ogiltigt JSON → retry 1x
  - Validering fallerar → retry med tydligare instruktion
  - Timeout / API nere → markera `needsHumanReview = true`, skapa utan klassificering
  - Rate limit → exponential backoff
- [ ] **Loggning** — Logga varje prompt + svar (utan PII), latency, token-förbrukning
- [ ] **Felhantering** — Global exception handler i controller-lagret
- [ ] **Databasmigrering** — Plan för att byta InMemoryRepository mot riktig DB (t.ex. EF Core + SQLite/PostgreSQL)

> **OBS:** CORS och enhetstester har flyttats upp till Fas 1 resp. Fas 2 — de behövs redan där.

---

## Exekveringsordning

| # | Fas | Beskrivning | Beroende |
|---|-----|-------------|----------|
| 1 | Modeller + buggfixar | PipelineStep, uppdatera Incident/IncidentAnalysis/IncidentConstants, trådsäker repo, CORS, fixa Program.cs-dubblett | — |
| 2 | Service + validering | Tvåstegsflöde i IncidentService, PATCH-validering, GET /api/constants, enhetstester för statuslogik | Fas 1 |
| 3 | Fake AI | CredibilityGateway fake-impl, fixa AiGateway (rätt casing, ta bort credibility-fält) | Fas 2 |
| 4 | Frontend | Kortvy, PipelineSteps, nya statusar, fixa CreateAiRequest-typ, operatörsknappar, hämta constants från API | Fas 3 |
| 5 | Riktig AI | Microsoft.Extensions.AI, prompts, structured output, validering | Fas 3 |
| 6 | Produktion | Retry, loggning, global felhantering, DB-migrering | Fas 5 |

Fas 4 (frontend) och Fas 5 (riktig AI) kan köras parallellt efter Fas 3.

---

## Körinstruktioner

```bash
# Backend
cd server
dotnet run
# → http://localhost:5236/swagger

# Frontend
cd client
npm run dev
# → http://localhost:3000
```
