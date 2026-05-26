using Application.Interfaces;
using Domain.Models;
using Infrastructure.AI.Parsing;
using Infrastructure.AI.Prompts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Infrastructure.AI.Configuration;
using System.Text.Json;

public class CredibilityGateway : ICredibilityGateway
{
    private readonly IChatClient _chat;
    private readonly AiOptions _options;
    private readonly ILogger<CredibilityGateway> _logger;
    private static readonly JsonElement SchemaElement =
        JsonDocument.Parse(CredibilityPrompt.JsonSchema).RootElement.Clone();

    public CredibilityGateway(IChatClient chat, IOptions<AiOptions> options, ILogger<CredibilityGateway> logger)
    {
        _chat = chat;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CredibilityAssessment> AssessAsync(
        string description,
        List<string> services,
        string priority,
        string createdBy)
    {
        var userMessage = $"""
            Beskrivning: "{description}"
            Tjänster: [{string.Join(", ", services)}]
            Prioritet: {priority}
            Skapad av: {createdBy}
            """;

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, CredibilityPrompt.System),
            new(ChatRole.User, userMessage)
        };

        var chatOptions = new ChatOptions
        {
            Temperature = (float)_options.Temperature,
            ResponseFormat = ChatResponseFormat.ForJsonSchema(
                SchemaElement,
                "credibility_assessment")
        };

        for (int attempt = 0; attempt <= _options.MaxRetries; attempt++)
        {
            var response = await _chat.GetResponseAsync(messages, chatOptions);
            var text = response.Text ?? "";

            _logger.LogDebug("Credibility attempt {Attempt}: {Response}", attempt, text);

            var result = AiResponseValidator.ValidateCredibility(text);
            if (result is not null)
            {
                var (credibility, needsHumanReview, reasoning) = result.Value;
                return new CredibilityAssessment(credibility, needsHumanReview, reasoning);
            }

            _logger.LogWarning("Credibility validation failed on attempt {Attempt}", attempt);
        }

        throw new InvalidOperationException(
            $"AI credibility assessment failed after {_options.MaxRetries + 1} attempts");
    }
}
