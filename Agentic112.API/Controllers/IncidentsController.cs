namespace Agentic112.API.Controllers;

using Agentic112.Domain.Constants;
using Agentic112.Domain.DTOS;
using Agentic122.Application.Interfaces;
using Agentic122.Application.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/incidents")]
public class IncidentsController : ControllerBase
{
    private readonly IncidentService _incidentService;
    private readonly IIncidentRepository _repo;

    public IncidentsController(IncidentService service, IIncidentRepository repo)
    {
        _incidentService = service;
        _repo = repo;
    }

    [HttpPost("manual")]
    public async Task<IActionResult> CreateManual([FromBody] CreateManualRequest request)
    {
        var result = await _incidentService.CreateManualAsync(
            request.Description,
            request.Services,
            request.Priority
        );

        return Ok(result);
    }

    [HttpPost("ai")]
    public async Task<IActionResult> CreateAi([FromBody] CreateAiRequest request)
    {
        var result = await _incidentService.CreateFromAiAsync(request.Description);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _repo.GetAllAsync();
        return Ok(list);
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateIncidentRequest request)
    {
        var incident = await _repo.GetByIdAsync(id);
        if (incident is null) return NotFound();

        if (request.Status is not null && !IncidentConstants.Statuses.Contains(request.Status))
            return BadRequest($"Invalid status. Allowed: {string.Join(", ", IncidentConstants.Statuses)}");

        // Manual edit only - no automatic AI validation here
        var descriptionChanged = request.Description is not null && request.Description != incident.Description;
        var wasAiCreated = incident.CreatedBy == "AI";
        var contentChanged = request.Description is not null || request.Services is not null || request.Priority is not null;

        if (request.Services is not null) incident.Services = request.Services;
        if (request.Priority is not null) incident.Priority = request.Priority;
        if (request.Description is not null)
        {
            incident.Description = request.Description;
            incident.CreatedBy = "User";
        }

        // AI-ärende med ändrad beskrivning → kör om hela pipelinen automatiskt
        if (wasAiCreated && descriptionChanged)
        {
            var result = await _incidentService.ValidateAsync(incident);
            return Ok(result);
        }

        if (contentChanged)
        {
            incident.Steps.Clear();
            incident.Confidence = null;
            incident.Credibility = null;
            incident.NeedsHumanReview = null;
            incident.Status = "pending_review";
        }

        if (request.Status is not null) incident.Status = request.Status;

        await _repo.UpdateAsync(incident);
        return Ok(incident);
    }

    [HttpPost("{id:guid}/validate")]
    public async Task<IActionResult> Validate(Guid id, [FromBody] ValidateIncidentRequest request)
    {
        var incident = await _repo.GetByIdAsync(id);
        if (incident is null) return NotFound();

        if (request.Services is not null) incident.Services = request.Services;
        if (request.Priority is not null) incident.Priority = request.Priority;
        if (request.Description is not null)
        {
            incident.Description = request.Description;
            incident.CreatedBy = "User";
        }

        var result = await _incidentService.ValidateAsync(incident);
        return Ok(result);
    }

    [HttpGet("constants")]
    public IActionResult GetConstants()
    {
        return Ok(new
        {
            services = IncidentConstants.Services,
            priorities = IncidentConstants.Priorities,
            statuses = IncidentConstants.Statuses,
            credibilityLevels = IncidentConstants.CredibilityLevels
        });
    }
}