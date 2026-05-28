namespace Agentic112.Domain.DTOS;

public record CreateManualRequest(
    string Description,
    List<string> Services,
    string Priority
);

public record CreateAiRequest(
    string Description
);

public record UpdateIncidentRequest(
    string? Description,
    List<string>? Services,
    string? Priority,
    string? Status
);

public record ValidateIncidentRequest(
    string? Description,
    List<string>? Services,
    string? Priority
);