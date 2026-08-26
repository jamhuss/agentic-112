using Agentic112.Domain.Constants;
using Agentic112.Domain.Entities;
using Agentic112.Domain.Models;
using Agentic112.Application.Interfaces;
using Microsoft.Extensions.Logging;


namespace Agentic112.Application.Services;

public class IncidentService(
        IAiGateway ai,
        ICredibilityGateway credibility,
        IIncidentRepository repo,
        ILogger<IncidentService> logger)
{

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
            Status = IncidentConstants.StatusOngoing,
            CreatedBy = IncidentConstants.CreatedByUser,
            CreatedAt = DateTime.UtcNow
        };

        await repo.SaveAsync(incident);

        return incident;
    }

    public async Task<Incident> CreateFromAiAsync(string description)
    {
        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            Description = description,
            Services = [],
            Priority = IncidentConstants.PriorityLow,
            CreatedBy = IncidentConstants.CreatedByAI,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            var analysis = await ai.AnalyzeAsync(description);

            incident.Services = analysis.Services;
            incident.Priority = analysis.Priority;
            incident.Confidence = analysis.Confidence;

            incident.Steps.Add(new PipelineStep(
                IncidentConstants.StepClassification,
                $"services: [{string.Join(", ", analysis.Services)}], priority: {analysis.Priority}, confidence: {analysis.Confidence}",
                analysis.Reasoning,
                DateTime.UtcNow
            ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Classification failed for AI incident");

            FlagForHumanReview(incident, IncidentConstants.StepClassification, $"Classification failed: {ex.Message}");

            await repo.SaveAsync(incident);
            return incident;
        }

        await repo.SaveAsync(incident);
        await RunCredibilityCheck(incident);
        await repo.UpdateAsync(incident);

        return incident;
    }

    public async Task<Incident> ValidateAsync(Incident incident)
    {
        incident.Steps.Clear();

        try
        {
            // Use AI's validation response which includes suggested services and explicit missing/extra lists
            var validation = await ai.ValidateAsync(incident.Description, incident.Services, incident.Priority);

            incident.Confidence = validation.Confidence;

            var aiSuggestedServices = validation.AiSuggestedServices ?? [];
            var missingServices = validation.MissingServices ?? [];
            var extraServices = validation.ExtraServices ?? [];
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
                IncidentConstants.StepClassificationValidation,
                validationResult,
                validationReasoning,
                DateTime.UtcNow
            ));

            await RunCredibilityCheck(incident);

            if (aiSuggestedServices.Count == 0 || !servicesMatch || !priorityMatch || incident.Credibility != IncidentConstants.CredibilityHigh)
            {
                incident.NeedsHumanReview = true;
                incident.Status = IncidentConstants.StatusFlagged;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Validation failed for incident {Id}", incident.Id);

            FlagForHumanReview(incident, IncidentConstants.StepClassificationValidation, $"Validation failed: {ex.Message}");
        }

        await repo.UpdateAsync(incident);

        return incident;
    }

    private async Task RunCredibilityCheck(Incident incident)
    {
        try
        {
            var assessment = await credibility.AssessAsync(
                incident.Description,
                incident.Services,
                incident.Priority,
                incident.CreatedBy);

            incident.Credibility = assessment.Credibility;
            incident.NeedsHumanReview = assessment.NeedsHumanReview;

            incident.Steps.Add(new PipelineStep(
                IncidentConstants.StepCredibilityCheck,
                $"credibility: {assessment.Credibility}, needsHumanReview: {assessment.NeedsHumanReview}",
                assessment.Reasoning,
                DateTime.UtcNow
            ));

            incident.Status = DetermineStatus(assessment.Credibility, incident.Confidence);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Credibility check failed for incident {Id}", incident.Id);

            FlagForHumanReview(incident, IncidentConstants.StepCredibilityCheck, $"Credibility check failed: {ex.Message}");
        }
    }

    private static string DetermineStatus(string credibility, double? confidence)
    {
        var effectiveConfidence = confidence ?? 1.0;

        return credibility switch
        {
            IncidentConstants.CredibilityHigh => IncidentConstants.StatusOngoing,
            IncidentConstants.CredibilityMedium when effectiveConfidence >= 0.6 => IncidentConstants.StatusOngoing,
            IncidentConstants.CredibilityMedium => IncidentConstants.StatusFlagged,
            IncidentConstants.CredibilityLow => IncidentConstants.StatusFlagged,
            _ => IncidentConstants.StatusFlagged
        };
    }

    private static void FlagForHumanReview(Incident incident, string flagName, string reason)
    {
        incident.Credibility = null;
        incident.NeedsHumanReview = true;
        incident.Status = IncidentConstants.StatusFlagged;
        incident.Steps.Add(new PipelineStep(
            flagName,
            "ERROR",
            reason,
            DateTime.UtcNow
        ));
    }
}