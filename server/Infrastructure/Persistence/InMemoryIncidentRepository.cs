using Application.Interfaces;
using Domain.Entities;

public class InMemoryIncidentRepository : IIncidentRepository
{
    private readonly List<Incident> _storage = new();

    public Task SaveAsync(Incident incident)
    {
        _storage.Add(incident);
        return Task.CompletedTask;
    }

    public Task<List<Incident>> GetAllAsync()
    {
        return Task.FromResult(_storage);
    }

    public Task<Incident?> GetByIdAsync(Guid id)
    {
        var incident = _storage.FirstOrDefault(i => i.Id == id);
        return Task.FromResult(incident);
    }

    public Task UpdateAsync(Incident incident)
    {
        var index = _storage.FindIndex(i => i.Id == incident.Id);
        if (index >= 0) _storage[index] = incident;
        return Task.CompletedTask;
    }
}