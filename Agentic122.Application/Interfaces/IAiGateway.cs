using Agentic112.Domain.Models;

namespace Agentic122.Application.Interfaces;

public interface IAiGateway
{
    Task<IncidentAnalysis> AnalyzeAsync(string description, List<string>? userSelectedServices = null, string? userSelectedPriority = null);
    Task<IncidentValidation> ValidateAsync(string description, List<string>? userSelectedServices = null, string? userSelectedPriority = null);
}