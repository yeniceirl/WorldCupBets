using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using WorldCupBets.Application.Abstractions;
using WorldCupBets.Application.Features.Auth;
using WorldCupBets.Domain.Common;

namespace WorldCupBets.Infrastructure.Authentication;

public sealed class GoogleTokenValidator(IConfiguration configuration) : IGoogleTokenValidator
{
    public async Task<Result<GoogleIdentity>> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            return Result<GoogleIdentity>.Failure(new Error("auth.google_token_required", "Google ID token is required."));
        }

        try
        {
            _ = cancellationToken;

            var validationSettings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [GetClientId()]
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings);
            var identity = new GoogleIdentity(
                payload.Subject,
                payload.Email,
                payload.Name ?? payload.Email,
                payload.EmailVerified);

            return Result<GoogleIdentity>.Success(identity);
        }
        catch (InvalidJwtException)
        {
            return Result<GoogleIdentity>.Failure(new Error("auth.invalid_google_token", "The Google token could not be validated."));
        }
    }

    private string GetClientId()
    {
        var clientId = configuration["Google:ClientId"];
        return string.IsNullOrWhiteSpace(clientId)
            ? throw new InvalidOperationException("Google:ClientId must be configured.")
            : clientId;
    }
}
