# Master Plan — Agentic-112

## Vision

Ett ärendehanteringssystem för nödsituationer med **tvåstegs AI-pipeline**: klassificering + trovärdighetsbedömning. Alla ärenden — manuella och AI-skapade — genomgår trovärdighetskontroll. Varje steg loggas och visualiseras transparent i frontend.

---

## Nuläge (uppdaterat 2026-05-26)

> Fas 1–5 är implementerade och verifierade. Systemet kör end-to-end med riktig AI (Azure OpenAI GPT-4o).

### Backend (`/server`) ✅

- **Domain/Entities/Incident.cs** — Id, Description, Services, Priority, Status(`"pending_review"`), CreatedBy, Confidence, Credibility, NeedsHumanReview, `List<PipelineStep> Steps`, CreatedAt
- **Domain/Models/IncidentAnalysis.cs** — `record(Services, Priority, Confidence?, Reasoning)`
- **Domain/Models/CredibilityAssessment.cs** — `record(Credibility, NeedsHumanReview, Reasoning)`
- **Domain/Models/PipelineStep.cs** — `record(Name, Result, Reasoning, Timestamp)`
- **Domain/DTOS/** — CreateManualRequest, CreateAiRequest, UpdateIncidentRequest
- **Domain/Contstants/IncidentConstants.cs** — Services, Priorities, CredibilityLevels, Statuses
- **Application/Interfaces/** — IAiGateway, ICredibilityGateway, IIncidentRepository
- **Application/Services/IncidentService.cs** — Tvåstegsflöde: klassificering + trovärdighetskontroll, statuslogik, felhantering med fallback till flagged
- **Infrastructure/AI/AiGateway.cs** — Riktig GPT-4o via IChatClient, structured output, retry + validering
- **Infrastructure/AI/CredibilityGateway.cs** — Riktig GPT-4o via IChatClient, structured output, retry + validering
- **Infrastructure/AI/Prompts/ClassificationPrompt.cs** — System prompt + JSON schema för klassificering
- **Infrastructure/AI/Prompts/CredibilityPrompt.cs** — System prompt + JSON schema för trovärdighet
- **Infrastructure/AI/Parsing/AiResponseValidator.cs** — Validerar AI-svar mot IncidentConstants
- **Infrastructure/AI/Configuration/AiOptions.cs** — Endpoint, Model, Temperature, MaxRetries, ApiKey
- **Infrastructure/Persistence/InMemoryIncidentRepository.cs** — Trådsäker med `lock`
- **Api/Controllers/IncidentsController.cs** — POST manual, POST ai, GET all, PATCH {id} (med statusvalidering), GET constants
- **Program.cs** — DI (IChatClient via AzureOpenAIClient, IAiGateway, ICredibilityGateway, IncidentService), CORS, Swagger
- **appsettings.json** — AI-sektion (Endpoint, Model, Temperature, MaxRetries)
- **User Secrets** — AI:ApiKey (aldrig i git)

### Frontend (`/client`) ✅

- React + TypeScript + Vite, proxy `/api` → `http://localhost:5236`
- **Kortvy** med IncidentCard-komponenter (ersätter tidigare tabellvy)
- **PipelineSteps.tsx** — Visar ✅/❌ per steg med reasoning
- **IncidentCard.tsx** — Metadata, status-badge, pipeline-steg, Godkänn/Avvisa-knappar för flaggade
- **CreateIncidentModal** — Manuellt + AI-läge (AI visar bara beskrivningsfält)
- **EditIncidentModal** — Alla fem statusar, services valfritt
- **labels.ts** — Svenska labels för alla statusar (pending_review, ongoing, flagged, rejected, closed)
- **api.ts** — fetchIncidents, createManualIncident, createAgenticIncident (bara description), updateIncident
- **types.ts** — Incident (med steps), PipelineStep, CreateManualRequest, CreateAiRequest, UpdateIncidentRequest

---

## Fas 1 — Omstrukturering av modeller + buggfixar

**Mål:** Separera klassificering från trovärdighetskontroll. Introducera PipelineStep för spårbarhet. Fixa kända buggar.

### Uppgifter

- [x] **Domain/Models/IncidentAnalysis.cs** — Ta bort `Credibility`, `NeedsHumanReview`. Gör Confidence nullable (`double?`). Behåll: `Services`, `Priority`, `Confidence`, `Reasoning`
- [x] **Domain/Models/PipelineStep.cs** — NY modell:
  ```csharp
  public record PipelineStep(
      string Name,       // "classification" eller "credibility_check"
      string Result,     // strukturerad sammanfattning, t.ex. "services: [police], priority: high"
      string Reasoning,  // AI:ns motivering i fritext
      DateTime Timestamp
  );
  ```
  > **OBS:** `Result` är fritext-sammanfattning av stegets output. Hålls enkelt nu, kan bli strukturerat objekt i Fas 5 om det behövs.
- [x] **Domain/Entities/Incident.cs** — Lägg till `List<PipelineStep> Steps = new()`, ändra `Status` default till `"pending_review"`
- [x] **Domain/Contstants/IncidentConstants.cs** — Lägg till:
  ```csharp
  public static readonly List<string> Statuses = new()
  {
      "pending_review", "ongoing", "flagged", "rejected", "closed"
  };
  ```
- [x] **Infrastructure/Persistence/InMemoryIncidentRepository.cs** — Gör trådsäker: lägg till `lock` runt alla operationer på `_storage`
- [x] **Program.cs** — Ta bort dubbletten av `AddEndpointsApiExplorer()`
- [x] **Program.cs** — Lägg till CORS-konfiguration (`AllowAnyOrigin` i dev, explicit origin i prod)

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

- [x] **Application/Services/IncidentService.cs** — Injicera `ICredibilityGateway`, implementera tvåstegsflöde enligt ovan, inkl. felhantering
- [x] **Program.cs** — Registrera `ICredibilityGateway` i DI
- [x] **Api/Controllers/IncidentsController.cs** — PATCH: validera `request.Status` mot `IncidentConstants.Statuses` innan uppdatering, returnera `400 Bad Request` vid ogiltigt värde
- [x] **Api/Controllers/IncidentsController.cs** — NY endpoint: `GET /api/constants` — returnerar tillgängliga services, priorities, statuses, credibilityLevels (så frontend inte hårdkodar)
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

- [x] **Infrastructure/AI/AiGateway.cs** — Uppdatera: returnera `IncidentAnalysis` UTAN credibility-fält. **Fixa värden:** använd `"ambulance"` (inte `"Ambulans"`), `"high"` (inte `"High"`) — måste matcha `IncidentConstants`
- [x] **Infrastructure/AI/CredibilityGateway.cs** — NY implementation av `ICredibilityGateway`:
  - Beskrivning innehåller "test" / "hej" / "asdf" → `low`, `needsHumanReview: true`, `"Ser ut som ett test"`
  - Beskrivning < 10 tecken → `low`, `needsHumanReview: true`, `"För kort beskrivning för att bedöma"`
  - Annars → `high`, `needsHumanReview: false`, `"Realistisk och trovärdig beskrivning"`
  > **OBS:** Fake-implementationen använder bara `description` för att avgöra. Parametrarna `services`, `priority`, `createdBy` skickas med men används först i Fas 5 (riktig AI).
- [x] Verifiera: skapa manuellt ärende → får credibility-steg
- [x] Verifiera: skapa AI-ärende → får classification + credibility-steg
- [x] Verifiera: "test"-ärende → status `flagged`
- [x] Verifiera: vanligt ärende → status `ongoing`

---

## Fas 4 — Frontend-ombyggnad

**Mål:** Byt från tabellvy till kortvy med pipeline-visualisering.

### Nya/ändrade filer

- [x] **types.ts** — Lägg till `PipelineStep` interface, lägg till `steps: PipelineStep[]` på `Incident`. Lägg till separat `CreateAiRequest` typ (bara `{ description }`)
- [x] **labels.ts** — Uppdatera `STATUS_LABELS`:
  ```typescript
  export const STATUS_LABELS: Record<string, string> = {
    pending_review: "Granskas",
    ongoing: "Pågående",
    flagged: "Flaggad",
    rejected: "Avvisad",
    closed: "Avslutat",
  };
  ```
- [x] **api.ts** — Fixa `createAgenticIncident`: använd `CreateAiRequest` (bara description), inte `CreateManualRequest`
- [x] **components/PipelineSteps.tsx** — NY komponent:
  - Renderar `steps[]` med ✅/❌ ikoner
  - Visar reasoning för varje steg
  - Manuella ärenden: visar "Manuellt skapat" istället för classification-steg
- [x] **components/IncidentCard.tsx** — NY komponent (ersätter tabellrad):
  - Beskrivning + metadata (tjänster, prioritet, datum, skapad av)
  - Status-badge med färgkodning
  - Pipeline-steg (PipelineSteps)
  - Edit-knapp
  - Flaggade ärenden: "Godkänn" / "Avvisa"-knappar (anropar PATCH med `status: "ongoing"` resp. `status: "rejected"`)
- [x] **components/IncidentList.tsx** — Ändra till att rendera `IncidentCard` istället för tabell
- [x] **components/EditIncidentModal.tsx** — Uppdatera status-dropdown med alla fem statusar. Fixa: gör services-validering valfri (tillåt submit utan services vid redigering)
- [x] **App.css** — Nya styles:
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

- [x] **server.csproj** — Lägg till `Microsoft.Extensions.AI.OpenAI` NuGet-paket
- [x] **Infrastructure/AI/Configuration/AiOptions.cs** — NY:
  ```csharp
  public class AiOptions
  {
      public string Endpoint { get; set; } = "";
      public string Model { get; set; } = "gpt-4o";
      public double Temperature { get; set; } = 0.2;
      public int MaxRetries { get; set; } = 2;
  }
  ```
- [x] **appsettings.json** — Lägg till AI-sektion (utan hemligheter)
- [x] **User Secrets** — `dotnet user-secrets set "AI:ApiKey" "..."`
- [x] **Infrastructure/AI/Prompts/ClassificationPrompt.cs** — NY:
  - System prompt: "Du är en SOS Alarm-operatör..."
  - JSON schema som tvingar: services (enum), priority (enum), confidence (0-1), reasoning
- [x] **Infrastructure/AI/Prompts/CredibilityPrompt.cs** — NY:
  - System prompt: "Bedöm trovärdigheten..."
  - JSON schema: credibility (enum), needsHumanReview (bool), reasoning
- [x] **Infrastructure/AI/Parsing/AiResponseValidator.cs** — NY:
  - Validera services mot `IncidentConstants.Services`
  - Validera priority mot `IncidentConstants.Priorities`
  - Validera credibility mot `IncidentConstants.CredibilityLevels`
  - Om ogiltigt → retry 1x → fallback till `flagged` + `needsHumanReview = true`
- [x] **Infrastructure/AI/AiGateway.cs** — Omskriv med `IChatClient`, prompt, structured output
- [x] **Infrastructure/AI/CredibilityGateway.cs** — Omskriv med `IChatClient`, prompt, structured output
- [x] **Program.cs** — Registrera `IChatClient`, bind `AiOptions` från config

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

| # | Fas | Beskrivning | Status |
|---|-----|-------------|--------|
| 1 | Modeller + buggfixar | PipelineStep, uppdatera Incident/IncidentAnalysis/IncidentConstants, trådsäker repo, CORS, fixa Program.cs-dubblett | ✅ Klar |
| 2 | Service + validering | Tvåstegsflöde i IncidentService, PATCH-validering, GET /api/constants | ✅ Klar |
| 3 | Fake AI | CredibilityGateway fake-impl, fixa AiGateway (rätt casing, ta bort credibility-fält) | ✅ Klar |
| 4 | Frontend | Kortvy, PipelineSteps, nya statusar, fixa CreateAiRequest-typ, operatörsknappar | ✅ Klar |
| 5 | Riktig AI | Microsoft.Extensions.AI, prompts, structured output, validering | ✅ Klar |
| 6 | Produktion | Retry, loggning, global felhantering, DB-migrering | ⬜ Nästa |

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
