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
	- OREALISTISKA/OMÖJLIGA scenarion (drakar, zombies, magi etc): sätt normalt `low` priority och låg `confidence`.
	- Om scenariot är fysiskt omöjligt ska prioriteten ALDRIG vara `critical` eller `high`.
	- Viktigt: om inga tjänster rekommenderas (t.ex. busringning eller uppenbart orealistiskt) ska AI returnera en TOM ARRAY i fältet `services` (dvs. []). Systemet förlitar sig på den tomma listan och gör inte egna hårdkodade trösklar i service-lagret.

**Vid validering av operatörens val:**
- AI:n kan anropas via `ValidateAsync` och returnerar ett strukturerat valideringsresultat med följande fält: `aiSuggestedServices`, `missingServices`, `extraServices`, `suggestedPriority`, `confidence`, `reasoning`.
- Systemet använder AI:s listor direkt: om `aiSuggestedServices` är icke-tom kan tjänster korrigeras automatiskt; om `aiSuggestedServices` är tom behålls operatörens val och ärendet markeras för manuell granskning.
- Granskar om prioritet är rimlig och kan korrigera den till `suggestedPriority`.
- `reasoning` ska vara kort vid OK (max 15 ord) och något längre vid avvikelse.

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