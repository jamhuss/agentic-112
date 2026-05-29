using System.Text.Json;
using Agentic112.Domain.Constants;
using Agentic112.Domain.Models;

namespace Agentic112.AI.Parsing;

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

            // Allow empty services list (e.g., prank or unrealistic scenarios).
            // Previously an empty list was treated as invalid; now we accept it and
            // return an empty list so the caller can decide how to handle it.
            if (!IncidentConstants.Priorities.Contains(priority)) return null;
            if (confidence < 0 || confidence > 1) confidence = Math.Clamp(confidence, 0, 1);

            return (services, priority, confidence, reasoning);
        }
        catch
        {
            return null;
        }
    }

    public static IncidentValidation? ValidateValidation(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var aiSuggested = root.GetProperty("aiSuggestedServices")
                .EnumerateArray()
                .Select(s => s.GetString()!)
                .Where(s => IncidentConstants.Services.Contains(s))
                .ToList();

            var missing = root.GetProperty("missingServices")
                .EnumerateArray()
                .Select(s => s.GetString()!)
                .Where(s => IncidentConstants.Services.Contains(s))
                .ToList();

            var extra = root.GetProperty("extraServices")
                .EnumerateArray()
                .Select(s => s.GetString()!)
                .Where(s => IncidentConstants.Services.Contains(s))
                .ToList();

            var suggestedPriority = root.GetProperty("suggestedPriority").GetString()!;
            var confidence = root.GetProperty("confidence").GetDouble();
            var reasoning = root.GetProperty("reasoning").GetString() ?? string.Empty;

            if (!IncidentConstants.Priorities.Contains(suggestedPriority)) return null;
            if (confidence < 0 || confidence > 1) confidence = Math.Clamp(confidence, 0, 1);

            return new IncidentValidation(aiSuggested, missing, extra, suggestedPriority, confidence, reasoning);
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
