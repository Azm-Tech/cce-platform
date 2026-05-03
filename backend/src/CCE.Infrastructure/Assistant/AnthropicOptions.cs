namespace CCE.Infrastructure.Assistant;

/// <summary>
/// Configuration shape for the Anthropic LLM client. Bound from
/// Configuration["Assistant:Anthropic"]; missing keys fall back to
/// the defaults declared here.
/// </summary>
public sealed record AnthropicOptions
{
    public string Model { get; init; } = "claude-sonnet-4-5-20250929";
    public int MaxTokens { get; init; } = 1024;
    public double Temperature { get; init; } = 0.3;
}
