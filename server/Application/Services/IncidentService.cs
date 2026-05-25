using Application.Interfaces;
using Domain.Entities;

namespace Application.Services;

public class IncidentService
{
    private readonly IAiGateway _ai;
    private readonly IIncidentRepository _repo;

    public IncidentService(IAiGateway ai, IIncidentRepository repo)
    {
        _ai = ai;
        _repo = repo;
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
        var analysis = await _ai.AnalyzeAsync(description);

        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            Description = description,
            Services = analysis.Services,
            Priority = analysis.Priority,
            CreatedBy = "AI",
            Confidence = analysis.Confidence,
            Credibility = analysis.Credibility,
            NeedsHumanReview = analysis.NeedsHumanReview,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.SaveAsync(incident);
        return incident;
    }
}