namespace Agentic112.Domain.Models;

public record PipelineStep(
    string Name,
    string Result,
    string Reasoning,
    DateTime Timestamp
);
