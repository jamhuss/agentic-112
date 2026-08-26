using Agentic112.AI;
using Agentic112.AI.Configuration;
using Agentic112.Application.Interfaces;
using Agentic112.Application.Services;
using Agentic112.Infrastructure.Persistence;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AI Incident API",
        Version = "v1",
        Description = "API for incident classification (manual vs AI)"
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// AI Configuration
var aiSection = builder.Configuration.GetSection("AI");
builder.Services.Configure<AiOptions>(aiSection);

var aiOptions = aiSection.Get<AiOptions>()
    ?? throw new InvalidOperationException("Konfigurationssektionen 'AI' saknas.");

if (string.IsNullOrWhiteSpace(aiOptions.AzureOpenAIEndpoint))
    throw new InvalidOperationException("'AI:AzureOpenAIEndpoint' är inte konfigurerad.");

if (string.IsNullOrWhiteSpace(aiOptions.Model))
    throw new InvalidOperationException("'AI:Model' är inte konfigurerad.");

// Keyless authentication: DefaultAzureCredential locally (VS/Azure CLI login),
// ManagedIdentityCredential in Azure App Service for faster, predictable auth.
TokenCredential credential = builder.Environment.IsDevelopment()
    ? new DefaultAzureCredential()
    : string.IsNullOrWhiteSpace(aiOptions.ManagedIdentityClientId)
        ? new ManagedIdentityCredential()
        : new ManagedIdentityCredential(aiOptions.ManagedIdentityClientId);

// Talk directly to the Azure OpenAI endpoint using the AzureOpenAIClient
var azureClient = new AzureOpenAIClient(new Uri(aiOptions.AzureOpenAIEndpoint), credential);

builder.Services.AddSingleton<IChatClient>(
    azureClient.GetChatClient(aiOptions.Model).AsIChatClient());

// DI
builder.Services.AddSingleton<IIncidentRepository, InMemoryIncidentRepository>();
builder.Services.AddSingleton<IAiGateway, AiGateway>();
builder.Services.AddSingleton<ICredibilityGateway, CredibilityGateway>();
builder.Services.AddScoped<IncidentService>();

var app = builder.Build();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();

// Serve React SPA from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

// SPA fallback – all non-API routes serve index.html
app.MapFallbackToFile("index.html");

app.Run();
