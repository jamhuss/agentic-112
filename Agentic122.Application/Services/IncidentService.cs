using Agentic112.Domain.Entities;
using Agentic112.Domain.Models;
using Agentic122.Application.Interfaces;
using Microsoft.Extensions.Logging;


namespace Agentic122.Application.Services;

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
            Status = "ongoing",
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
            // Use AI's validation response which includes suggested services and explicit missing/extra lists
            var validation = await _ai.ValidateAsync(incident.Description, incident.Services, incident.Priority);

            incident.Confidence = validation.Confidence;

            var aiSuggestedServices = validation.AiSuggestedServices ?? new List<string>();
            var missingServices = validation.MissingServices ?? new List<string>();
            var extraServices = validation.ExtraServices ?? new List<string>();
            var servicesMatch = missingServices.Count == 0 && extraServices.Count == 0;

            // AI produces the structured summary for this step
            var validationResult = validation.Summary;

            var priorityMatch = incident.Priority == validation.SuggestedPriority;

            // Use AI's reasoning as authoritative for this validation step
            var validationReasoning = validation.Reasoning;

            // Korrigera prioritet till AI:ns bedömning
            if (!priorityMatch)
            {
                incident.Priority = validation.SuggestedPriority;
            }

            // Korrigera tjänster till AI:ns bedömning
            // Only auto-correct services when AI actually suggests services.
            if (!servicesMatch && aiSuggestedServices.Count > 0)
            {
                incident.Services = aiSuggestedServices;
            }

            incident.Steps.Add(new PipelineStep(
                "classification_validation",
                validationResult,
                validationReasoning,
                DateTime.UtcNow
            ));

            await RunCredibilityCheck(incident);

            if (aiSuggestedServices.Count == 0 || !servicesMatch || !priorityMatch || incident.Credibility != "high")
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