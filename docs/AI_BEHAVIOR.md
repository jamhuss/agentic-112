# AI Behavior

## Roller

Systemet har **två AI-steg** med separata ansvarsområden:

### Steg 1 — Klassificering (IAiGateway)

AI:n agerar som en nödcentral-operatör (SOS Alarm). Den ska:

- Klassificera situationen utifrån fritextbeskrivning
- Tilldela rätt tjänster (`ambulance`, `police`, `fire_department`, `assistance`)
- Sätta prioritet (`critical`, `high`, `medium`, `low`)
- Ange confidence (0.0–1.0) för hur säker den är
- Motivera sitt beslut i `reasoning`

**Begränsningar:**
- Får ALDRIG hitta på nya tjänster utanför listan
- Får ALDRIG returnera fritext — alltid structured JSON
- Multipla tjänster tillåtet vid komplexa situationer

### Steg 2 — Trovärdighetsbedömning (ICredibilityGateway)

AI:n bedömer om ärendet är trovärdigt. Körs på **alla** ärenden (manuella + AI-klassificerade). Den ska:

- Bedöma om beskrivningen är fysiskt möjlig och realistisk
- Detektera orealistiska scenarion (potentiella falsklarm)
- Sätta credibility-nivå
- Bestämma om mänsklig granskning krävs
- Motivera sitt beslut i `reasoning`

## Credibility-nivåer

| Nivå | Betydelse | Konsekvens |
|------|-----------|------------|
| `high` | Realistisk, trovärdig beskrivning | → `ongoing`, ingen granskning |
| `medium` | Oklar situation | → `ongoing` om confidence ≥ 0.6, annars `flagged` |
| `low` | Troligt falsklarm, fysiskt omöjligt | → `flagged`, kräver granskning |

## Operatörsflöde för flaggade ärenden

Flaggade ärenden (`status: "flagged"`) visas med tydlig motivering i frontend. Operatören kan:

- **Godkänna** → status ändras till `ongoing`
- **Avvisa** → status ändras till `rejected`

## Felhantering

Om AI:n inte kan nås eller returnerar ogiltigt svar:
- Ärendet sparas ändå
- Sätts till `status: "flagged"`, `needsHumanReview: true`
- Pipeline-steg loggas med felmeddelande