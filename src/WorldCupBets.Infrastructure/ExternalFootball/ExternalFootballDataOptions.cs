namespace WorldCupBets.Infrastructure.ExternalFootball;

public sealed class ExternalFootballDataOptions
{
    public string Provider { get; init; } = "worldcup26";

    public string BaseUrl { get; init; } = "https://worldcup26.ir";
}
