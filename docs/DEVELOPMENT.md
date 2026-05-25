# Development Guidelines

## Architecture

Clean architecture backend with AI as a two-step pipeline component, and a React frontend.

### Backend structure (`/server`)

- **Api** → Controllers (HTTP endpoints, no business logic)
- **Application** → Services + interfaces (business logic, orchestration)
- **Domain** → Entities, models, DTOs, constants (no dependencies)
- **Infrastructure** → AI gateways + persistence (implementation details)

### Frontend structure (`/client`)

- React + TypeScript + Vite
- Proxy `/api` → backend at `http://localhost:5236`
- Kortvy med pipeline-visualisering per ärende

---

## Core Rule

All incident creation must result in the SAME `Incident` entity.

There are two sources:

1. **Manual** (user-defined services, priority)
2. **AI** (model-classified from free text)

Both flows pass through a **two-step pipeline**:
1. **Klassificering** (AI only) — `IAiGateway.AnalyzeAsync()` → services, priority, confidence
2. **Trovärdighetskontroll** (alla ärenden) — `ICredibilityGateway.AssessAsync()` → credibility, needsHumanReview

---

## Incident Model

All flows produce:

- Description
- Services (must be from `IncidentConstants.Services`)
- Priority (must be from `IncidentConstants.Priorities`)
- Status (`pending_review` → `ongoing` / `flagged` → `rejected` / `closed`)
- CreatedBy (`"User"` or `"AI"`)
- Steps (`List<PipelineStep>` — audit trail)

AI-klassificering lägger till:
- Confidence (0.0–1.0, nullable — null för manuella)

Trovärdighetskontroll lägger till:
- Credibility (`high` / `medium` / `low`)
- NeedsHumanReview

---

## Pipeline Steps (PipelineStep)

Varje steg i pipelinen loggas som en `PipelineStep`:

| Fält | Beskrivning |
|------|-------------|
| `Name` | `"classification"` eller `"credibility_check"` |
| `Result` | Sammanfattning av stegets output |
| `Reasoning` | AI:ns motivering i fritext |
| `Timestamp` | När steget kördes |

---

## Status Lifecycle

```
pending_review → ongoing     (trovärdig)
pending_review → flagged     (låg trovärdighet / AI-fel)
flagged        → ongoing     (operatör godkänner)
flagged        → rejected    (operatör avvisar)
ongoing        → closed      (ärende avslutat)
```

---

## AI Integration — Two Gateways

### IAiGateway (klassificering)

Ansvar:
- Analysera fritext
- Returnera `IncidentAnalysis` (services, priority, confidence, reasoning)
- Returnerar **aldrig** credibility (det är ett separat steg)

### ICredibilityGateway (trovärdighetsbedömning)

Ansvar:
- Bedöma trovärdigheten i ett ärende
- Returnera `CredibilityAssessment` (credibility, needsHumanReview, reasoning)
- Körs på **alla** ärenden (manuella + AI)

---

## AI Constraints

AI:n MÅSTE:

- Bara använda tillåtna services: `ambulance`, `police`, `fire_department`, `assistance`
- Bara använda tillåtna priorities: `critical`, `high`, `medium`, `low`
- Alltid returnera structured JSON
- Aldrig returnera fritext istället för JSON
- Aldrig hitta på nya tjänster eller prioriteter

---

## Validation Rules

Alla AI-svar måste valideras:

- Services måste finnas i `IncidentConstants.Services`
- Priority måste finnas i `IncidentConstants.Priorities`
- Credibility måste finnas i `IncidentConstants.CredibilityLevels`
- Status vid PATCH måste finnas i `IncidentConstants.Statuses`
- Om validering misslyckas → retry 1x → fallback till `flagged` + `needsHumanReview = true`

---

## Statuslogik

| Credibility | Confidence | → Status | → NeedsHumanReview |
|-------------|-----------|----------|---------------------|
| `high` | any | `ongoing` | `false` |
| `medium` | ≥ 0.6 | `ongoing` | `false` |
| `medium` | < 0.6 | `flagged` | `true` |
| `low` | any | `flagged` | `true` |

> Manuella ärenden har `Confidence = null`. Behandla null som 1.0.

---

## Coding Rules

- Ingen business logic i controllers
- AI-anrop bara via `IAiGateway` / `ICredibilityGateway`
- Inga direkta LLM-anrop i services
- Alla modeller enkla och explicita
- Constants centraliserade i `IncidentConstants`
- Frontend hämtar constants från `GET /api/constants` (inte hårdkodade)
- InMemoryRepository måste vara trådsäker (lock/concurrent)

---

## Development Strategy

1. ~~Fake AI-svar~~ ✅ (befintlig AiGateway)
2. Tvåstegsflöde med fake CredibilityGateway
3. Frontend med kortvy och pipeline-visualisering
4. Riktig AI med `Microsoft.Extensions.AI` + Azure OpenAI
5. Produktionsredo: retry, loggning, felhantering

Se [MASTER_PLAN.md](MASTER_PLAN.md) för detaljerad fasplan.

The goal is NOT:
- to build the smartest AI

The goal IS:
- to build a correct system around AI