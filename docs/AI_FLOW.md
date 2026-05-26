# AI Flow — Från request till svar

## AI-ärende (POST /api/incidents/ai)

```mermaid
sequenceDiagram
    participant U as Användare
    participant FE as Frontend (React)
    participant API as Controller
    participant SVC as IncidentService
    participant AI as AiGateway (GPT-4o)
    participant CRED as CredibilityGateway (GPT-4o)
    participant DB as InMemoryRepository

    U->>FE: Skriver "det brinner i ett lagerhus"
    FE->>API: POST /api/incidents/ai { description }
    API->>SVC: CreateFromAiAsync(description)
    
    Note over SVC,AI: STEG 1 — Klassificering
    SVC->>AI: AnalyzeAsync(description)
    AI->>AI: Bygger prompt (system + user message)
    AI->>AI: Anropar GPT-4o med JSON Schema
    AI->>AI: Validerar svar mot IncidentConstants
    AI-->>SVC: IncidentAnalysis(services, priority, confidence, reasoning)
    
    SVC->>SVC: Skapar Incident (status: "pending_review")
    SVC->>SVC: Lägger till PipelineStep("classification", ...)
    SVC->>DB: SaveAsync(incident)
    
    Note over SVC,CRED: STEG 2 — Trovärdighetskontroll
    SVC->>CRED: AssessAsync(description, services, priority, "AI")
    CRED->>CRED: Bygger prompt med all kontext
    CRED->>CRED: Anropar GPT-4o med JSON Schema
    CRED->>CRED: Validerar svar
    CRED-->>SVC: CredibilityAssessment(credibility, needsHumanReview, reasoning)
    
    SVC->>SVC: Lägger till PipelineStep("credibility_check", ...)
    SVC->>SVC: DetermineStatus(credibility, confidence)
    SVC->>DB: UpdateAsync(incident)
    
    SVC-->>API: Incident (komplett)
    API-->>FE: 200 OK + JSON
    FE->>FE: Renderar IncidentCard med pipeline-steg
```

---

## Manuellt ärende (POST /api/incidents/manual)

Samma flöde men **utan Steg 1** (klassificering):

1. Användaren väljer tjänster + prioritet själv
2. `POST /api/incidents/manual` → `CreateManualAsync(description, services, priority)`
3. Incident skapas med `CreatedBy: "User"`, `Confidence: null`
4. **Hoppar direkt till Steg 2** (trovärdighetskontroll)
5. Status bestäms: null confidence behandlas som 1.0 → bara credibility avgör

---

## Statuslogik (DetermineStatus)

| Credibility | Confidence | → Status |
|-------------|-----------|----------|
| `high` | any | `ongoing` |
| `medium` | ≥ 0.6 | `ongoing` |
| `medium` | < 0.6 | `flagged` |
| `low` | any | `flagged` |

> Manuella ärenden: Confidence = null → behandlas som 1.0

---

## Felhantering

Om GPT-4o-anropet i trovärdighetschecken misslyckas:

1. Incident sparas ändå
2. Status → `"flagged"`, NeedsHumanReview → `true`
3. PipelineStep loggas med `"ERROR"` + felmeddelande
4. Exception loggas via ILogger

---

## Operatörsflöde i frontend

Varje kort visar:
- Beskrivning, metadata, status-badge
- **Pipeline-steg** med ✅/❌ och AI:ns motivering
- Flaggade ärenden → **Godkänn/Avvisa-knappar** (PATCH status → `ongoing` / `rejected`)

---

## Statuslivscykel

```
pending_review → ongoing     (trovärdig)
pending_review → flagged     (låg trovärdighet / AI-fel)
flagged        → ongoing     (operatör godkänner)
flagged        → rejected    (operatör avvisar)
ongoing        → closed      (ärende avslutat)
```
