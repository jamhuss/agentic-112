using Application.Interfaces;
using Domain.Models;

public class AiGateway : IAiGateway
{
    public Task<IncidentAnalysis> AnalyzeAsync(string description)
    {
        // TODO: connect to Copilot / GPT / Claude

        // Fake response for now
        var result = new IncidentAnalysis(
            Services: new List<string> { "Ambulans" },
            Priority: "High",
            Confidence: 0.85,
            Credibility: "Medium",
            NeedsHumanReview: false,
            Reasoning: "Detected possible medical emergency"
        );

        return Task.FromResult(result);
    }
}