namespace CCE.Infrastructure.Moderation;

public sealed class ModerationOptions
{
    public const string SectionName = "Moderation";

    /// <summary>"Ollama" | "OpenAiCompatible" | "None"</summary>
    public string Provider { get; set; } = "None";

    /// <summary>When true, auto-rejected content is immediately soft-deleted.</summary>
    public bool AutoRejectOnViolation { get; set; }

    /// <summary>Case-insensitive keyword denylist evaluated by the rule-based pre-filter.</summary>
    public string[] DenyList { get; set; } = [];

    public OllamaOptions Ollama { get; set; } = new();
    public OpenAiCompatibleOptions OpenAiCompatible { get; set; } = new();
}

public sealed class OllamaOptions
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model   { get; set; } = "llama3.2";
}

public sealed class OpenAiCompatibleOptions
{
    public string BaseUrl { get; set; } = "https://api.groq.com/openai";
    public string ApiKey  { get; set; } = string.Empty;
    public string Model   { get; set; } = "llama-3.1-8b-instant";
}
