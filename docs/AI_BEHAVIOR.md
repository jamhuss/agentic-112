# AI Behavior

## Roller

Systemet har **två AI-steg** med separata ansvarsområden:

### Steg 1 — Klassificering (IAiGateway)

AI:n agerar som en erfaren nödcentral-operatör. Den ska:

- Klassificera situationen utifrån fritextbeskrivning
- Tilldela rätt tjänster (`ambulance`, `police`, `fire_department`, `assistance`)
- Sätta prioritet (`critical`, `high`, `medium`, `low`)
- Ange confidence (0.0–1.0) för hur säker den är
- Motivera kort och naturligt på svenska i `reasoning`
- Orealistiska/omöjliga scenarion: ALLTID low priority + confidence < 0.2

**Vid validering av operatörens val:**
- Granskar om valda tjänster är korrekta och tillräckliga
- Granskar om prioritet är rimlig
- Korrigerar tjänster och prioritet om de avviker
- Kort reasoning vid OK (max 15 ord), längre förklaring vid avvikelse

**Begränsningar:**
- Får ALDRIG hitta på nya tjänster utanför listan
- Får ALDRIG returnera fritext — alltid structured JSON
- Multipla tjänster tillåtet vid komplexa situationer

### Steg 2 — Trovärdighetsbedömning (ICredibilityGateway)

AI:n bedömer om ärendet är trovärdigt. Den ska:

- Bedöma om beskrivningen är fysiskt möjlig och realistisk
- Detektera orealistiska scenarion (potentiella falsklarm)
- Sätta credibility-nivå
- Bestämma om mänsklig granskning krävs
- Kort reasoning vid hög trovärdighet, längre vid misstänkt

## När körs AI?

| Scenario | AI-pipeline | Trigger |
|----------|------------|--------|
| Nytt AI-ärende | Full (klassificering + trovärdighet) | Automatiskt |
| Nytt manuellt ärende | Ingen | — |
| Redigera manuellt ärende | Ingen (status → pending_review) | — |
| Redigera AI-ärende (ny beskrivning) | Full (validering + trovärdighet) | Automatiskt |
| "Validera med AI"-knappen | Full (validering + trovärdighet) | Explicit |

## Credibility-nivåer

| Nivå | Betydelse | Konsekvens |
|------|-----------|------------|
| `high` | Realistisk, trovärdig beskrivning | → `ongoing`, ingen granskning |
| `medium` | Oklar situation | → `ongoing` om confidence ≥ 0.6, annars `flagged` |
| `low` | Troligt falsklarm, fysiskt omöjligt | → `flagged`, kräver granskning |

## Validering: flaggning och korrigering

Vid explicit validering eller AI-redigering:
- Tjänster korrigeras till AI:ns bedömning
- Prioritet korrigeras till AI:ns bedömning
- Status sätts till `flagged` om tjänster/prioritet avvek ELLER trovärdighet är låg/medium

## Operatörsflöde för flaggade ärenden

Flaggade ärenden (`status: "flagged"`) visas med tydlig motivering. Operatören kan:

- **Godkänna** → status ändras till `ongoing`
- **Avvisa** → status ändras till `rejected`

## Felhantering

Om AI:n inte kan nås eller returnerar ogiltigt svar:
- Ärendet sparas ändå
- Sätts till `status: "flagged"`, `needsHumanReview: true`
- Pipeline-steg loggas med felmeddelande