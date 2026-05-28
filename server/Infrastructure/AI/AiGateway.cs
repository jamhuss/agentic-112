using Application.Interfaces;
using Infrastructure.AI.Parsing;
using Infrastructure.AI.Prompts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Infrastructure.AI.Configuration;
using System.Text.Json;
using Agentic112.Domain.Models;

public class AiGateway : IAiGateway
{
    private readonly IChatClient _chat;
    private readonly AiOptions _options;
    private readonly ILogger<AiGateway> _logger;
    private static readonly JsonElement SchemaElement =
        JsonDocument.Parse(ClassificationPrompt.JsonSchema).RootElement.Clone();

    public AiGateway(IChatClient chat, IOptions<AiOptions> options, ILogger<AiGateway> logger)
    {
        _chat = chat;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IncidentAnalysis> AnalyzeAsync(string description, List<string>? userSelectedServices = null, string? userSelectedPriority = null)
    {
        var userMessage = description;
        if (userSelectedServices is { Count: > 0 } || userSelectedPriority is not null)
        {
            var parts = new List<string>();
            if (userSelectedServices is { Count: > 0 })
                parts.Add($"Operatörens valda tjänster: [{string.Join(", ", userSelectedServices)}]");
            if (userSelectedPriority is not null)
                parts.Add($"Operatörens valda prioritet: {userSelectedPriority}");
            userMessage += $"\n\n{string.Join(". ", parts)}. Granska om valen är korrekta.";
        }

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, ClassificationPrompt.System),
            new(ChatRole.User, userMessage)
        };

        var chatOptions = new ChatOptions
        {
            Temperature = (float)_options.Temperature,
            ResponseFormat = ChatResponseFormat.ForJsonSchema(
                SchemaElement,
                "classification")
        };

        for (int attempt = 0; attempt <= _options.MaxRetries; attempt++)
        {
            var response = await _chat.GetResponseAsync(messages, chatOptions);
            var text = response.Text ?? "";

            _logger.LogDebug("Classification attempt {Attempt}: {Response}", attempt, text);

            var result = AiResponseValidator.ValidateClassification(text);
            if (result is not null)
            {
                var (services, priority, confidence, reasoning) = result.Value;
                return new IncidentAnalysis(services, priority, confidence, reasoning);
            }

            _logger.LogWarning("Classification validation failed on attempt {Attempt}", attempt);
        }

        throw new InvalidOperationException(
            $"AI classification failed after {_options.MaxRetries + 1} attempts for: \"{description}\"");
    }
}

