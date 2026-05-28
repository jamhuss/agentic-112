# Agentic-112 — AI Incident Classification System

## Purpose

Ärendehanteringssystem för nödsituationer med **tvåstegs AI-pipeline**:
1. **Klassificering** — AI kategoriserar fritext till tjänster + prioritet
2. **Trovärdighetsbedömning** — Alla ärenden bedöms för trovärdighet

Varje pipeline-steg loggas och visualiseras transparent i frontenden.

## Flöden

### Manuellt
Användare anger tjänster + prioritet manuellt → trovärdighetskontroll körs automatiskt.

### AI (Agentic)
Användare skriver fritext → AI klassificerar → trovärdighetskontroll körs automatiskt.

## Statuslivscykel

```
pending_review → ongoing     (trovärdig)
pending_review → flagged     (låg trovärdighet / AI-fel)
flagged        → ongoing     (operatör godkänner)
flagged        → rejected    (operatör avvisar)
ongoing        → closed      (ärende avslutat)
```

## Tech stack

| Lager | Teknik |
|-------|--------|
| Backend | .NET 10 Web API, Clean Architecture |
| Frontend | React + TypeScript + Vite |
| AI | Microsoft.Extensions.AI + Azure OpenAI GPT-4o |
| AI-format | Structured Outputs (JSON Schema) |

## Kom igång

```bash
# Backend
cd Agentic112.API
dotnet user-secrets set "AI:ApiKey" "din-azure-openai-nyckel"
dotnet run
# → http://localhost:5236/swagger

# Frontend
cd Agentic112.Client
npm install
npm run dev
# → http://localhost:112
```

## Dokumentation

- [docs/MASTER_PLAN.md](docs/MASTER_PLAN.md) — Fasindelad plan med status
- [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md) — Arkitektur och riktlinjer
- [docs/AI_BEHAVIOR.md](docs/AI_BEHAVIOR.md) — AI-beteende och regler
- [docs/AI_FLOW.md](docs/AI_FLOW.md) — Flödesdiagram från request till svar
- [docs/TEST_SCENARIOS.md](docs/TEST_SCENARIOS.md) — Testscenarier (legit + falska)
- [docs/PROJECT_SETUP.md](docs/PROJECT_SETUP.md) — Setup-guide