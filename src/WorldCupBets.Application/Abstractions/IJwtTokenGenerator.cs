namespace WorldCupBets.Application.Abstractions;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(Guid userId);
}
