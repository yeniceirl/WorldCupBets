using WorldCupBets.Application.Abstractions;

namespace WorldCupBets.Infrastructure.Authentication;

public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    public string GenerateAccessToken(Guid userId)
    {
        _ = userId;
        throw new NotSupportedException("JWT token generation is a scaffold placeholder and is not implemented yet.");
    }
}
