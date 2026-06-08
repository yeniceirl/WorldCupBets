namespace WorldCupBets.Infrastructure.AiInsights;

public sealed class AiInsightsOptions
{
    public string ApiKey { get; init; } = string.Empty;

    public string BaseUrl { get; init; } = "https://opencode.ai/zen/go/v1";

    public string Model { get; init; } = "qwen3.7-plus";

    public int TimeoutSeconds { get; init; } = 60;

    public int MaxTokens { get; init; } = 700;
}
