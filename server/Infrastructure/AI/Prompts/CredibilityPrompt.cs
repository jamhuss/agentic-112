namespace Infrastructure.AI.Prompts;

public static class CredibilityPrompt
{
    public const string System = """
        Du är en trovärdighetsbedömare för nödsamtal. Din uppgift är att avgöra om en rapporterad nödsituation verkar trovärdig.

        Du ska bedöma:
        1. Är beskrivningen fysiskt möjlig och realistisk?
        2. Finns det tecken på falsklarm, skämt eller test?
        3. Är beskrivningen tillräckligt detaljerad?

        TILLÅTNA TROVÄRDIGHTESNIVÅER:
        - high — Realistisk och trovärdig beskrivning
        - medium — Oklar situation, kan vara äkta men osäkert
        - low — Troligt falsklarm, test eller orealistiskt scenario

        REGLER:
        - Svara ALLTID med giltig JSON enligt schemat nedan
        - needsHumanReview = true om du är osäker eller credibility inte är "high"
        - Motivera kort på svenska i "reasoning"
        - Var skeptisk mot extremt korta beskrivningar eller uppenbara testmeddelanden
        """;

    public const string JsonSchema = """
        {
          "type": "object",
          "properties": {
            "credibility": {
              "type": "string",
              "enum": ["high", "medium", "low"]
            },
            "needsHumanReview": {
              "type": "boolean"
            },
            "reasoning": {
              "type": "string"
            }
          },
          "required": ["credibility", "needsHumanReview", "reasoning"],
          "additionalProperties": false
        }
        """;
}
