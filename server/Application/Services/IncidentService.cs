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
        var analysis = await _ai.AnalyzeAsync(description, services);

        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            Description = description,
            Services = analysis.Services,
            Priority = analysis.Priority,
            Confidence = analysis.Confidence,
            CreatedBy = "User",
            CreatedAt = DateTime.UtcNow
        };

        incident.Steps.Add(new PipelineStep(
            "classification",
            $"services: [{string.Join(", ", analysis.Services)}], priority: {analysis.Priority}, confidence: {analysis.Confidence}",
            analysis.Reasoning,
            DateTime.UtcNow
        ));

        await _repo.SaveAsync(incident);
        await RunCredibilityCheck(incident);
        await _repo.UpdateAsync(incident);

        return incident;
    }

    public async Task<Incident> CreateFromAiAsync(string description)
    {
        var analysis = await _ai.AnalyzeAsync(description);

        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            Description = description,
            Services = analysis.Services,
            Priority = analysis.Priority,
            CreatedBy = "AI",
            Confidence = analysis.Confidence,
            CreatedAt = DateTime.UtcNow
        };

        incident.Steps.Add(new PipelineStep(
            "classification",
            $"services: [{string.Join(", ", analysis.Services)}], priority: {analysis.Priority}, confidence: {analysis.Confidence}",
            analysis.Reasoning,
            DateTime.UtcNow
        ));

        await _repo.SaveAsync(incident);
        await RunCredibilityCheck(incident);
        await _repo.UpdateAsync(incident);

        return incident;
    }

    public async Task<Incident> ReclassifyAsync(Incident incident, string newDescription, List<string>? userSelectedServices = null)
    {
        incident.Description = newDescription;
        incident.CreatedBy = "User";
        incident.Steps.Clear();

        var analysis = await _ai.AnalyzeAsync(newDescription, userSelectedServices);

        incident.Services = analysis.Services;
        incident.Priority = analysis.Priority;
        incident.Confidence = analysis.Confidence;

        incident.Steps.Add(new PipelineStep(
            "classification",
            $"services: [{string.Join(", ", analysis.Services)}], priority: {analysis.Priority}, confidence: {analysis.Confidence}",
            analysis.Reasoning,
            DateTime.UtcNow
        ));

        await RunCredibilityCheck(incident);
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