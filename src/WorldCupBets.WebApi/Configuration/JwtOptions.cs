using System.Text;

namespace WorldCupBets.WebApi.Configuration;

public sealed class JwtOptions
{
    public const int MinimumSecretBytes = 32;

    public const int MinimumAccessTokenLifetimeMinutes = 5;

    public const int MaximumAccessTokenLifetimeMinutes = 120;

    public string Secret { get; init; } = string.Empty;

    public string Issuer { get; init; } = "WorldCupBets";

    public string Audience { get; init; } = "WorldCupBets.Frontend";

    public int AccessTokenLifetimeMinutes { get; init; } = 60;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Secret))
        {
            throw new InvalidOperationException("Jwt:Secret must be configured.");
        }

        if (Encoding.UTF8.GetByteCount(Secret) < MinimumSecretBytes)
        {
            throw new InvalidOperationException($"Jwt:Secret must be at least {MinimumSecretBytes} UTF-8 bytes for HS256 signing.");
        }

        if (string.IsNullOrWhiteSpace(Issuer))
        {
            throw new InvalidOperationException("Jwt:Issuer must be configured.");
        }

        if (string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("Jwt:Audience must be configured.");
        }

        if (AccessTokenLifetimeMinutes is < MinimumAccessTokenLifetimeMinutes or > MaximumAccessTokenLifetimeMinutes)
        {
            throw new InvalidOperationException($"Jwt:AccessTokenLifetimeMinutes must be between {MinimumAccessTokenLifetimeMinutes} and {MaximumAccessTokenLifetimeMinutes} minutes.");
        }
    }
}
