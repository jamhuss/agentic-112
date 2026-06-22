namespace Agentic112.Domain.Models;

public record IncidentValidation(
    List<string> AiSuggestedServices,
    List<string> MissingServices,
    List<string> ExtraServices,
    string SuggestedPriority,
    double Confidence,
    string Summary,
    string Reasoning
);
