using Agentic112.Domain.Entities;

namespace Agentic122.Application.Interfaces;

public interface IIncidentRepository
{
    Task SaveAsync(Incident incident);
    Task<List<Incident>> GetAllAsync();
    Task<Incident?> GetByIdAsync(Guid id);
    Task UpdateAsync(Incident incident);
}