namespace MZikmund.HeroImageGenerator.Services;

/// <summary>
/// Configuration options for OpenAI service.
/// </summary>
public class OpenAIOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? ImageSize { get; set; }
    public string? Quality { get; set; }
}
