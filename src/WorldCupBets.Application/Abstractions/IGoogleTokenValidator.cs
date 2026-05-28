using WorldCupBets.Application.Features.Auth;
using WorldCupBets.Domain.Common;

namespace WorldCupBets.Application.Abstractions;

public interface IGoogleTokenValidator
{
    Task<Result<GoogleIdentity>> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
}
