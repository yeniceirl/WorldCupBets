using WorldCupBets.Application.Abstractions;
using WorldCupBets.Domain.Common;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;

namespace WorldCupBets.Application.Features.Auth;

public sealed class GoogleLoginHandler
{
    public static async Task<Result<AuthResponseDto>> Handle(
        GoogleLoginCommand command,
        IGoogleTokenValidator googleTokenValidator,
        IJwtTokenGenerator jwtTokenGenerator,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        CancellationToken cancellationToken)
    {
        var validationResult = await googleTokenValidator.ValidateAsync(command.IdToken, cancellationToken);
        if (validationResult.IsFailure || validationResult.Value is null)
        {
            return Result<AuthResponseDto>.Failure(validationResult.Error ?? new Error("auth.invalid_google_token", "The Google token could not be validated."));
        }

        var googleIdentity = validationResult.Value;
        if (!googleIdentity.EmailVerified)
        {
            return Result<AuthResponseDto>.Failure(new Error("auth.email_not_verified", "The Google account email must be verified."));
        }

        var user = await userRepository.GetByGoogleSubjectWithRolesAsync(googleIdentity.Subject, cancellationToken);
        if (user is null)
        {
            var bettorRole = await roleRepository.GetByNameAsync("Bettor", cancellationToken);
            if (bettorRole is null)
            {
                return Result<AuthResponseDto>.Failure(new Error("auth.role_not_found", "The Bettor role is not configured."));
            }

            user = User.Create(googleIdentity.Subject, googleIdentity.Email, googleIdentity.DisplayName);
            user.UserRoles.Add(UserRole.Create(user, bettorRole));
            await userRepository.AddAsync(user, cancellationToken);
            await userRepository.SaveChangesAsync(cancellationToken);
        }

        var roles = user.UserRoles.Select(userRole => userRole.Role.Name).Distinct(StringComparer.Ordinal).ToArray();
        var accessToken = jwtTokenGenerator.GenerateAccessToken(new AuthTokenContext(
            user.Id,
            user.Email,
            user.DisplayName,
            roles));

        return Result<AuthResponseDto>.Success(new AuthResponseDto(
            accessToken,
            new AuthenticatedUserDto(user.Id, user.Email, user.DisplayName, roles)));
    }
}
