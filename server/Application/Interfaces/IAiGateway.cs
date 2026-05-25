using Domain.Models;

namespace Application.Interfaces;

public interface IAiGateway
{
    Task<IncidentAnalysis> AnalyzeAsync(string description);
}