using Agentic112.Domain.Models;

namespace Agentic112.Application.Interfaces;

public interface IAiGateway
{
    Task<IncidentAnalysis> AnalyzeAsync(string description);
    Task<IncidentValidation> ValidateAsync(string description, List<string>? userSelectedServices = null, string? userSelectedPriority = null);
}