using WorldCupBets.Application.Abstractions;

namespace WorldCupBets.Infrastructure.Authentication;

public sealed class GoogleTokenValidator : IGoogleTokenValidator
{
    public Task<bool> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
    {
        _ = idToken;
        _ = cancellationToken;
        throw new NotSupportedException("Google token validation is a scaffold placeholder and is not implemented yet.");
    }
}
