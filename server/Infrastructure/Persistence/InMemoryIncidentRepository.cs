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
}