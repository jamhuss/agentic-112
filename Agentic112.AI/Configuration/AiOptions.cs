namespace Agentic112.AI.Configuration;

public class AiOptions
{
    public string Endpoint { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gpt-4o";
    public double Temperature { get; set; } = 0.2;
    public int MaxRetries { get; set; } = 2;
}
