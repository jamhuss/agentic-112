
using Agentic112.Domain.Entities;
using Agentic122.Application.Interfaces;

public class InMemoryIncidentRepository : IIncidentRepository
{
    private readonly List<Incident> _storage = new();
    private readonly object _lock = new();

    public Task SaveAsync(Incident incident)
    {
        lock (_lock)
        {
            _storage.Add(incident);
        }
        return Task.CompletedTask;
    }

    public Task<List<Incident>> GetAllAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_storage.ToList());
        }
    }

    public Task<Incident?> GetByIdAsync(Guid id)
    {
        lock (_lock)
        {
            var incident = _storage.FirstOrDefault(i => i.Id == id);
            return Task.FromResult(incident);
        }
    }

    public Task UpdateAsync(Incident incident)
    {
        lock (_lock)
        {
            var index = _storage.FindIndex(i => i.Id == incident.Id);
            if (index >= 0) _storage[index] = incident;
        }
        return Task.CompletedTask;
    }
}