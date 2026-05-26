using Application.Interfaces;
using Domain.Models;

public class AiGateway : IAiGateway
{
    public Task<IncidentAnalysis> AnalyzeAsync(string description)
    {
        // Fake classification - picks services/priority based on keywords
        var services = new List<string>();
        var priority = "low";
        double confidence = 0.7;

        var lower = description.ToLowerInvariant();

        if (lower.Contains("brand") || lower.Contains("eld"))
        {
            services.Add("fire");
            priority = "high";
            confidence = 0.9;
        }
        else if (lower.Contains("olycka") || lower.Contains("skadad"))
        {
            services.Add("ambulance");
            priority = "high";
            confidence = 0.85;
        }
        else if (lower.Contains("inbrott") || lower.Contains("stöld"))
        {
            services.Add("police");
            priority = "medium";
            confidence = 0.75;
        }
        else
        {
            services.Add("police");
            confidence = 0.5;
        }

        var result = new IncidentAnalysis(
            Services: services,
            Priority: priority,
            Confidence: confidence,
            Reasoning: $"Fake classification based on keywords in: \"{description}\""
        );

        return Task.FromResult(result);
    }
}