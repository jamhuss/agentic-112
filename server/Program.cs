using Application.Interfaces;
using Application.Services;
using Azure.AI.OpenAI;
using Infrastructure.AI.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.OpenApi;
using System.ClientModel;

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

var aiOptions = aiSection.Get<AiOptions>()!;
var credential = new ApiKeyCredential(aiOptions.ApiKey);
var azureClient = new AzureOpenAIClient(new Uri(aiOptions.Endpoint), credential);

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
