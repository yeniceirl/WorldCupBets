using WorldCupBets.Application.Abstractions;
using WorldCupBets.Application.Features.Auth;
using WorldCupBets.Domain.Common;

namespace WorldCupBets.Infrastructure.Authentication;

public sealed class GoogleTokenValidator : IGoogleTokenValidator
{
    public Task<Result<GoogleIdentity>> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
    {
        _ = idToken;
        _ = cancellationToken;
        throw new NotSupportedException("Google token validation is a scaffold placeholder and is not implemented yet.");
    }
}
