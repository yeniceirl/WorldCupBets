namespace WorldCupBets.Infrastructure.Authentication;

public sealed class JwtAuthOptions
{
    public string Secret { get; init; } = string.Empty;

    public string Issuer { get; init; } = "WorldCupBets";

    public string Audience { get; init; } = "WorldCupBets.Frontend";

    public int AccessTokenLifetimeMinutes { get; init; } = 60;
}
