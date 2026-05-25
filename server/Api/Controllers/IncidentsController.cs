namespace server.Api.Controllers;

using Application.Interfaces;
using Application.Services;
using global::Domain.DTOS;
using Microsoft.AspNetCore.Mvc;

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

        if (request.Description is not null) incident.Description = request.Description;
        if (request.Services is not null) incident.Services = request.Services;
        if (request.Priority is not null) incident.Priority = request.Priority;
        if (request.Status is not null) incident.Status = request.Status;

        await _repo.UpdateAsync(incident);
        return Ok(incident);
    }
}