namespace Agentic112.AI.Configuration;

public class AiOptions
{
    /// <summary>Azure OpenAI endpoint (t.ex. https://&lt;resource&gt;.openai.azure.com/).</summary>
    public string AzureOpenAIEndpoint { get; set; } = "";

    /// <summary>Deployment-namnet för modellen i Foundry-projektet.</summary>
    public string Model { get; set; } = "gpt-4o";

    /// <summary>Valfritt client id för en user-assigned managed identity (används i produktion). Lämna tomt för system-assigned.</summary>
    public string ManagedIdentityClientId { get; set; } = "";

    public double Temperature { get; set; } = 0.2;
    public int MaxRetries { get; set; } = 2;
}
