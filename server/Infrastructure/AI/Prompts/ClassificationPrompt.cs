namespace Infrastructure.AI.Prompts;

public static class ClassificationPrompt
{
    public const string System = """
        Du är en SOS Alarm-operatör. Din uppgift är att klassificera nödsituationer baserat på en fritextbeskrivning.

        Du ska:
        1. Identifiera vilka tjänster som behövs
        2. Sätta en prioritetsnivå
        3. Ange hur säker du är (confidence 0.0–1.0)
        4. Motivera ditt beslut kort

        TILLÅTNA TJÄNSTER (välj en eller flera):
        - ambulance
        - police
        - fire_department
        - assistance

        TILLÅTNA PRIORITETER (välj exakt en):
        - critical
        - high
        - medium
        - low

        REGLER:
        - Använd BARA tjänster och prioriteter från listorna ovan
        - Svara ALLTID med giltig JSON enligt schemat nedan
        - Motivera kort på svenska i "reasoning"
        - Confidence 0.9+ = mycket tydligt fall, 0.5-0.7 = osäkert
        """;

    public const string JsonSchema = """
        {
          "type": "object",
          "properties": {
            "services": {
              "type": "array",
              "items": { "type": "string", "enum": ["ambulance", "police", "fire_department", "assistance"] }
            },
            "priority": {
              "type": "string",
              "enum": ["critical", "high", "medium", "low"]
            },
            "confidence": {
              "type": "number",
              "minimum": 0.0,
              "maximum": 1.0
            },
            "reasoning": {
              "type": "string"
            }
          },
          "required": ["services", "priority", "confidence", "reasoning"],
          "additionalProperties": false
        }
        """;
}
