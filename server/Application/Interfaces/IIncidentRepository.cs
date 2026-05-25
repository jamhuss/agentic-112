
using Domain.Entities;

namespace Application.Interfaces;

public interface IIncidentRepository
{
    Task SaveAsync(Incident incident);
    Task<List<Incident>> GetAllAsync();
}