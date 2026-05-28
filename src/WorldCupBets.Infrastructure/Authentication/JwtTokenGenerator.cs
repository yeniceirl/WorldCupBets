using WorldCupBets.Application.Abstractions;
using WorldCupBets.Application.Features.Auth;

namespace WorldCupBets.Infrastructure.Authentication;

public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    public string GenerateAccessToken(AuthTokenContext context)
    {
        _ = context;
        throw new NotSupportedException("JWT token generation is a scaffold placeholder and is not implemented yet.");
    }
}
