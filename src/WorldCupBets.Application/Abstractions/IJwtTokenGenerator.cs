using WorldCupBets.Application.Features.Auth;

namespace WorldCupBets.Application.Abstractions;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(AuthTokenContext context);
}
