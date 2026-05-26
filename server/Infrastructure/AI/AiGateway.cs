using Application.Interfaces;
using Domain.Models;
using Infrastructure.AI.Parsing;
using Infrastructure.AI.Prompts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Infrastructure.AI.Configuration;
using System.Text.Json;

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

    public async Task<IncidentAnalysis> AnalyzeAsync(string description)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, ClassificationPrompt.System),
            new(ChatRole.User, description)
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

