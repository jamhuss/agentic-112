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
| AI (nuvarande) | Fake keyword-baserad implementation |
| AI (planerad) | Microsoft.Extensions.AI + Azure OpenAI GPT-4o |

## Kom igång

```bash
# Backend
cd server
dotnet run
# → http://localhost:5236/swagger

# Frontend
cd client
npm install
npm run dev
# → http://localhost:3000
```

## Dokumentation

- [docs/MASTER_PLAN.md](docs/MASTER_PLAN.md) — Fasindelad plan med status
- [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md) — Arkitektur och riktlinjer
- [docs/AI_BEHAVIOR.md](docs/AI_BEHAVIOR.md) — AI-beteende och regler
- [docs/PROJECT_SETUP.md](docs/PROJECT_SETUP.md) — Setup-guide