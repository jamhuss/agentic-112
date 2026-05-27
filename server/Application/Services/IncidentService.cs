using Application.Interfaces;
using Domain.Entities;
using Domain.Models;

namespace Application.Services;

public class IncidentService
{
    private readonly IAiGateway _ai;
    private readonly ICredibilityGateway _credibility;
    private readonly IIncidentRepository _repo;
    private readonly ILogger<IncidentService> _logger;

    public IncidentService(
        IAiGateway ai,
        ICredibilityGateway credibility,
        IIncidentRepository repo,
        ILogger<IncidentService> logger)
    {
        _ai = ai;
        _credibility = credibility;
        _repo = repo;
        _logger = logger;
    }

    public async Task<Incident> CreateManualAsync(
        string description,
        List<string> services,
        string priority)
    {
        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            Description = description,
            Services = services,
            Priority = priority,
            CreatedBy = "User",
            CreatedAt = DateTime.UtcNow
        };

        await _repo.SaveAsync(incident);

        return incident;
    }

    public async Task<Incident> CreateFromAiAsync(string description)
    {
        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            Description = description,
            Services = new List<string>(),
            Priority = "Low",
            CreatedBy = "AI",
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            var analysis = await _ai.AnalyzeAsync(description);

            incident.Services = analysis.Services;
            incident.Priority = analysis.Priority;
            incident.Confidence = analysis.Confidence;

            incident.Steps.Add(new PipelineStep(
                "classification",
                $"services: [{string.Join(", ", analysis.Services)}], priority: {analysis.Priority}, confidence: {analysis.Confidence}",
                analysis.Reasoning,
                DateTime.UtcNow
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Classification failed for AI incident");

            incident.NeedsHumanReview = true;
            incident.Status = "flagged";

            incident.Steps.Add(new PipelineStep(
                "classification",
                "ERROR",
                $"Klassificering misslyckades: {ex.Message}",
                DateTime.UtcNow
            ));

            await _repo.SaveAsync(incident);
            return incident;
        }

        await _repo.SaveAsync(incident);
        await RunCredibilityCheck(incident);
        await _repo.UpdateAsync(incident);

        return incident;
    }

    public async Task<Incident> ValidateAsync(Incident incident)
    {
        incident.Steps.Clear();

        try
        {
            var analysis = await _ai.AnalyzeAsync(incident.Description, incident.Services, incident.Priority);
            incident.Confidence = analysis.Confidence;

            var selectedServices = incident.Services;
            var aiSuggestedServices = analysis.Services;
            var missingServices = aiSuggestedServices.Except(selectedServices).ToList();
            var extraServices = selectedServices.Except(aiSuggestedServices).ToList();
            var servicesMatch = missingServices.Count == 0 && extraServices.Count == 0;

            var validationResult =
                $"selectedServices: [{string.Join(", ", selectedServices)}], " +
                $"aiSuggestedServices: [{string.Join(", ", aiSuggestedServices)}], " +
                $"missing: [{string.Join(", ", missingServices)}], " +
                $"extra: [{string.Join(", ", extraServices)}], " +
                $"suggestedPriority: {analysis.Priority}, " +
                $"servicesMatch: {servicesMatch}";

            var priorityMatch = incident.Priority == analysis.Priority;

            var validationReasoning = analysis.Reasoning;
            if (!servicesMatch || !priorityMatch)
            {
                var issues = new List<string>();
                if (missingServices.Count > 0)
                    issues.Add($"Saknar: {string.Join(", ", missingServices)}");
                if (extraServices.Count > 0)
                    issues.Add($"Överflödiga: {string.Join(", ", extraServices)}");
                if (!priorityMatch)
                    issues.Add($"Prioritet korrigerad från {incident.Priority} till {analysis.Priority}");
                validationReasoning = $"{string.Join(". ", issues)}. {analysis.Reasoning}";
            }

            // Korrigera prioritet till AI:ns bedömning
            if (!priorityMatch)
            {
                incident.Priority = analysis.Priority;
            }

            // Korrigera tjänster till AI:ns bedömning
            if (!servicesMatch)
            {
                incident.Services = analysis.Services;
            }

            incident.Steps.Add(new PipelineStep(
                "classification_validation",
                validationResult,
                validationReasoning,
                DateTime.UtcNow
            ));

            await RunCredibilityCheck(incident);

            if (!servicesMatch || !priorityMatch || incident.Credibility != "high")
            {
                incident.NeedsHumanReview = true;
                incident.Status = "flagged";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation failed for incident {Id}", incident.Id);

            incident.NeedsHumanReview = true;
            incident.Status = "flagged";

            incident.Steps.Add(new PipelineStep(
                "classification_validation",
                "ERROR",
                $"Validering misslyckades: {ex.Message}",
                DateTime.UtcNow
            ));
        }

        await _repo.UpdateAsync(incident);

        return incident;
    }

    private async Task RunCredibilityCheck(Incident incident)
    {
        try
        {
            var assessment = await _credibility.AssessAsync(
                incident.Description,
                incident.Services,
                incident.Priority,
                incident.CreatedBy);

            incident.Credibility = assessment.Credibility;
            incident.NeedsHumanReview = assessment.NeedsHumanReview;

            incident.Steps.Add(new PipelineStep(
                "credibility_check",
                $"credibility: {assessment.Credibility}, needsHumanReview: {assessment.NeedsHumanReview}",
                assessment.Reasoning,
                DateTime.UtcNow
            ));

            incident.Status = DetermineStatus(assessment.Credibility, incident.Confidence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Credibility check failed for incident {Id}", incident.Id);

            incident.Credibility = null;
            incident.NeedsHumanReview = true;
            incident.Status = "flagged";

            incident.Steps.Add(new PipelineStep(
                "credibility_check",
                "ERROR",
                $"Trovärdighetskontroll misslyckades: {ex.Message}",
                DateTime.UtcNow
            ));
        }
    }

    private static string DetermineStatus(string credibility, double? confidence)
    {
        var effectiveConfidence = confidence ?? 1.0;

        return credibility switch
        {
            "high" => "ongoing",
            "medium" when effectiveConfidence >= 0.6 => "ongoing",
            "medium" => "flagged",
            "low" => "flagged",
            _ => "flagged"
        };
    }
}