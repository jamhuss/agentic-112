namespace Domain.Models;

public record CredibilityAssessment(
    string Credibility,
    bool NeedsHumanReview,
    string Reasoning
);
