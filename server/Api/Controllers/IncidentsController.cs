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
}