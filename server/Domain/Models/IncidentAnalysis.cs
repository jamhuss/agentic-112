

namespace Domain.Models;
public record IncidentAnalysis(
    List<string> Services,
    string Priority,
    double Confidence,
    string Credibility,
    bool NeedsHumanReview,
    string Reasoning
);