# AI Incident Classification System

## Purpose
This project demonstrates the difference between:

- Traditional incident creation (manual input)
- AI-driven incident classification (agentic flow)

## Key Idea
Users simulate emergency operators.

Two flows exist:

### Manual
User selects:
- services
- priority

### AI
User provides free-text:
- system uses AI to classify incident
- system assigns priority and credibility

---

## Example

Input:
"en person andas inte"

Output:
{
  "services": ["Ambulance"],
  "priority": "Critical",
  "credibility": "High",
  "confidence": 0.92
}

---

## Constraints

AI must:
- ONLY use predefined services
- return structured JSON
- never invent categories
``