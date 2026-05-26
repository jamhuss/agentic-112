using System.Text.Json;
using server.Domain.Constants;

namespace Infrastructure.AI.Parsing;

public static class AiResponseValidator
{
    public static (List<string> Services, string Priority, double Confidence, string Reasoning)?
        ValidateClassification(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var services = root.GetProperty("services")
                .EnumerateArray()
                .Select(s => s.GetString()!)
                .Where(s => IncidentConstants.Services.Contains(s))
                .ToList();

            var priority = root.GetProperty("priority").GetString()!;
            var confidence = root.GetProperty("confidence").GetDouble();
            var reasoning = root.GetProperty("reasoning").GetString() ?? "";

            if (services.Count == 0) return null;
            if (!IncidentConstants.Priorities.Contains(priority)) return null;
            if (confidence < 0 || confidence > 1) confidence = Math.Clamp(confidence, 0, 1);

            return (services, priority, confidence, reasoning);
        }
        catch
        {
            return null;
        }
    }

    public static (string Credibility, bool NeedsHumanReview, string Reasoning)?
        ValidateCredibility(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var credibility = root.GetProperty("credibility").GetString()!;
            var needsHumanReview = root.GetProperty("needsHumanReview").GetBoolean();
            var reasoning = root.GetProperty("reasoning").GetString() ?? "";

            if (!IncidentConstants.CredibilityLevels.Contains(credibility)) return null;

            return (credibility, needsHumanReview, reasoning);
        }
        catch
        {
            return null;
        }
    }
}
