namespace server.Api.Controllers;

using Application.Interfaces;
using Application.Services;
using global::Domain.DTOS;
using Microsoft.AspNetCore.Mvc;
using server.Domain.Constants;

[ApiController]
[Route("api/incidents")]
public class IncidentsController : ControllerBase
{
    private readonly IncidentService _service;
    private readonly IIncidentRepository _repo;

    public IncidentsController(IncidentService service, IIncidentRepository repo)
    {
        _service = service;
        _repo = repo;
    }

    [HttpPost("manual")]
    public async Task<IActionResult> CreateManual([FromBody] CreateManualRequest request)
    {
        var result = await _service.CreateManualAsync(
            request.Description,
            request.Services,
            request.Priority
        );

        return Ok(result);
    }

    [HttpPost("ai")]
    public async Task<IActionResult> CreateAi([FromBody] CreateAiRequest request)
    {
        var result = await _service.CreateFromAiAsync(request.Description);
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

        // Content edit → reclassify via AI pipeline
        if (request.Description is not null)
        {
            var result = await _service.ReclassifyAsync(incident, request.Description, request.Services);
            return Ok(result);
        }

        // Status-only update (approve/reject)
        if (request.Services is not null) incident.Services = request.Services;
        if (request.Priority is not null) incident.Priority = request.Priority;
        if (request.Status is not null) incident.Status = request.Status;

        await _repo.UpdateAsync(incident);
        return Ok(incident);
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