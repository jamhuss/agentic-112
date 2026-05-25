namespace Domain.DTOS;

public record CreateManualRequest(
    string Description,
    List<string> Services,
    string Priority
);

public record CreateAiRequest(
    string Description
);