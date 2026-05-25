# AI Incident Classification System – Project Setup Guide

## Goal

Build a backend system that demonstrates:

- Traditional incident creation (manual input)
- AI-driven incident classification

The AI must act as a backend component, not a UI feature.

---

## Step 1 – Initialize Repository

Run from root folder:

```bash
git init
dotnet new gitignore
```

## Step 2 – Create Solution

```bash
dotnet new sln -n agentic-112
```

## Step 3 – Create Backend Project

```bash
mkdir server
cd server
dotnet new webapi
```

## Step 4 – Clean Default Template

Delete:

- `WeatherForecast.cs`
- Default weather endpoint in `Program.cs`

## Step 5 – Add Project to Solution

Go back to root:

```bash
cd ..
dotnet sln add server/server.csproj
dotnet sln list
```

## Step 6 – Create Folder Structure

Inside `/server`:

```bash
mkdir Api
mkdir Application
mkdir Domain
mkdir Infrastructure
mkdir Api/Controllers
mkdir Application/Services
mkdir Application/Interfaces
mkdir Domain/Entities
mkdir Domain/Models
mkdir Domain/Constants
mkdir Infrastructure/AI
mkdir Infrastructure/Persistence
```

---

## Step 7 – Create Domain Model

**File:** `Domain/Entities/Incident.cs`

```csharp
public class Incident
{
    public Guid Id { get; set; }
    public string Description { get; set; } = "";
    public List<string> Services { get; set; } = new();
    public string Priority { get; set; } = "";
    public string CreatedBy { get; set; } = "User";
    public double? Confidence { get; set; }
    public string? Credibility { get; set; }
    public bool? NeedsHumanReview { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

## Step 8 – Create Incident Analysis Model

**File:** `Domain/Models/IncidentAnalysis.cs`

```csharp
public record IncidentAnalysis(
    List<string> Services,
    string Priority,
    double Confidence,
    string Credibility,
    bool NeedsHumanReview,
    string Reasoning
);
```

## Step 9 – Create Constants

**File:** `Domain/Constants/IncidentConstants.cs`

```csharp
public static class IncidentConstants
{
    public static readonly List<string> Services = new()
    {
        "ambulance",
        "police",
        "fire_department",
        "assistance"
    };

    public static readonly List<string> Priorities = new()
    {
        "critical",
        "high",
        "medium",
        "low"
    };

    public static readonly List<string> CredibilityLevels = new()
    {
        "high",
        "medium",
        "low"
    };
}
```

---

## Step 10 – Create Interfaces

**File:** `Application/Interfaces/IAiGateway.cs`

```csharp
public interface IAiGateway
{
    Task<IncidentAnalysis> AnalyzeAsync(string description);
}
```

**File:** `Application/Interfaces/IIncidentRepository.cs`

```csharp
public interface IIncidentRepository
{
    Task SaveAsync(Incident incident);
    Task<List<Incident>> GetAllAsync();
}
```

## Step 11 – Create Service

**File:** `Application/Services/IncidentService.cs`

```csharp
public class IncidentService
{
    private readonly IAiGateway _ai;
    private readonly IIncidentRepository _repo;

    public IncidentService(IAiGateway ai, IIncidentRepository repo)
    {
        _ai = ai;
        _repo = repo;
    }

    public async Task<Incident> CreateManualAsync(string description, List<string> services, string priority)
    {
        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            Description = description,
            Services = services,
            Priority = priority,
            CreatedBy = "User",
            CreatedAt = DateTime.UtcNow
        };

        await _repo.SaveAsync(incident);
        return incident;
    }

    public async Task<Incident> CreateFromAiAsync(string description)
    {
        var analysis = await _ai.AnalyzeAsync(description);

        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            Description = description,
            Services = analysis.Services,
            Priority = analysis.Priority,
            CreatedBy = "AI",
            Confidence = analysis.Confidence,
            Credibility = analysis.Credibility,
            NeedsHumanReview = analysis.NeedsHumanReview,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.SaveAsync(incident);
        return incident;
    }
}
```

---

## Step 12 – Create AI Gateway (MVP)

**File:** `Infrastructure/AI/AiGateway.cs`

```csharp
public class AiGateway : IAiGateway
{
    public Task<IncidentAnalysis> AnalyzeAsync(string description)
    {
        var result = new IncidentAnalysis(
            new List<string> { "ambulance" },
            "high",
            0.75,
            "medium",
            false,
            "Simulated result"
        );

        return Task.FromResult(result);
    }
}
```

## Step 13 – Create Repository

**File:** `Infrastructure/Persistence/InMemoryIncidentRepository.cs`

```csharp
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
```

---

## Step 14 – Controller

**File:** `Api/Controllers/IncidentsController.cs`

```csharp
[ApiController]
[Route("api/incidents")]
public class IncidentsController : ControllerBase
{
    private readonly IncidentService _service;
    private readonly IIncidentRepository _repo;

    public IncidentsController(IncidentService service, IIncidentRepository repo)
    {
        _service = service;
        _repo = repo;
    }

    [HttpPost("manual")]
    public async Task<IActionResult> CreateManual(CreateManualRequest request)
    {
        var result = await _service.CreateManualAsync(request.Description, request.Services, request.Priority);
        return Ok(result);
    }

    [HttpPost("ai")]
    public async Task<IActionResult> CreateAi(CreateAiRequest request)
    {
        var result = await _service.CreateFromAiAsync(request.Description);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _repo.GetAllAsync();
        return Ok(list);
    }
}
```

---

## Step 15 – Run

```bash
dotnet run
```

Open browser: `https://localhost:xxxx/swagger`

---

## Result

You now have:

- Working backend with Swagger UI
- Manual + AI incident creation endpoints
- PATCH endpoint for updating incidents
- Clean architecture (Api / Application / Domain / Infrastructure)

---

## Frontend Setup (React + Vite)

### Step 16 – Create Frontend Project

From root:

```bash
npm create vite@latest client -- --template react-ts
cd client
npm install
```

### Step 17 – Configure Vite Proxy

**File:** `client/vite.config.ts`

```typescript
export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    proxy: {
      '/api': {
        target: 'http://localhost:5236',
        changeOrigin: true,
      },
    },
  },
})
```

### Step 18 – Run Both

```bash
# Terminal 1 — Backend
cd server
dotnet run
# → http://localhost:5236/swagger

# Terminal 2 — Frontend
cd client
npm run dev
# → http://localhost:3000
```

---

## Current State

The project now has:

- Backend: manual + AI incident creation, edit, list
- Frontend: incident list (tabell), create modal (manuellt + agentic), edit modal
- Vite proxy eliminates CORS in development
- Swedish labels in frontend (Ambulans, Polis, Räddningstjänst, etc.)

---

## Next Steps

See [MASTER_PLAN.md](MASTER_PLAN.md) for the full roadmap:

1. **Fas 1** — PipelineStep model, trådsäker repository, CORS, buggfixar
2. **Fas 2** — Tvåstegsflöde (klassificering + trovärdighetskontroll)
3. **Fas 3** — Fake CredibilityGateway
4. **Fas 4** — Frontend kortvy med pipeline-visualisering
5. **Fas 5** — Riktig AI (Microsoft.Extensions.AI + Azure OpenAI)
6. **Fas 6** — Produktion (retry, loggning, DB)