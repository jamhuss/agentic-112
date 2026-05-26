using Application.Interfaces;
using Domain.Models;

public class CredibilityGateway : ICredibilityGateway
{
    public Task<CredibilityAssessment> AssessAsync(
        string description,
        List<string> services,
        string priority,
        string createdBy)
    {
        // Fake credibility logic
        var lower = description.ToLowerInvariant();

        if (lower == "test" || lower == "hej")
        {
            return Task.FromResult(new CredibilityAssessment(
                Credibility: "low",
                NeedsHumanReview: true,
                Reasoning: "Description appears to be a test message"
            ));
        }

        if (description.Length < 10)
        {
            return Task.FromResult(new CredibilityAssessment(
                Credibility: "low",
                NeedsHumanReview: true,
                Reasoning: "Description is too short to be credible"
            ));
        }

        return Task.FromResult(new CredibilityAssessment(
            Credibility: "high",
            NeedsHumanReview: false,
            Reasoning: "Description appears credible based on length and content"
        ));
    }
}
