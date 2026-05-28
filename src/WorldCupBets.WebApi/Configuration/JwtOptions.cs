namespace WorldCupBets.WebApi.Configuration;

public sealed class JwtOptions
{
    public string Secret { get; init; } = string.Empty;
}
