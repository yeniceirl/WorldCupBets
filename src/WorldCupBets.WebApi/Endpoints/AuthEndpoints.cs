using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Wolverine;
using WorldCupBets.Application.Abstractions;
using WorldCupBets.Application.Features.Auth;
using WorldCupBets.Domain.Common;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;
using WorldCupBets.WebApi.Configuration;

namespace WorldCupBets.WebApi.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/google", [AllowAnonymous] async (
            GoogleLoginRequest request,
            IMessageBus messageBus,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.IdToken))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.IdToken)] = ["Google ID token is required."]
                });
            }

            var result = await messageBus.InvokeAsync<Result<AuthResponseDto>>(
                new GoogleLoginCommand(request.IdToken),
                cancellationToken);

            if (result.IsFailure)
            {
                return result.Error?.Code switch
                {
                    "auth.invalid_google_token" => Results.Unauthorized(),
                    "auth.email_not_verified" => Results.Unauthorized(),
                    "auth.not_invited" => Results.Forbid(),
                    "auth.role_not_found" => Results.Problem(
                        title: "Authentication configuration error",
                        detail: result.Error.Message,
                        statusCode: StatusCodes.Status500InternalServerError),
                    _ => Results.BadRequest(new { error = result.Error?.Message ?? "Authentication failed." })
                };
            }

            return Results.Ok(result.Value);
        })
        .WithName("GoogleLogin")
        .WithSummary("Exchange a Google ID token for a WorldCupBets JWT.")
        .Produces<AuthResponseDto>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/dev-login", [AllowAnonymous] async (
            DevLoginRequest request,
            IHostEnvironment hostEnvironment,
            IOptions<AuthOptions> authOptions,
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IJwtTokenGenerator jwtTokenGenerator,
            CancellationToken cancellationToken) =>
        {
            if (!hostEnvironment.IsDevelopment() || !authOptions.Value.EnableDevLogin)
            {
                return Results.NotFound();
            }

            var googleSubject = string.IsNullOrWhiteSpace(request.GoogleSubject)
                ? "dev-bettor"
                : request.GoogleSubject.Trim();
            var email = string.IsNullOrWhiteSpace(request.Email)
                ? "dev-bettor@worldcupbets.local"
                : request.Email.Trim();
            var displayName = string.IsNullOrWhiteSpace(request.DisplayName)
                ? "Dev Bettor"
                : request.DisplayName.Trim();
            var requestedRoleName = string.Equals(request.Role, "Admin", StringComparison.OrdinalIgnoreCase)
                ? "Admin"
                : "Bettor";

            var user = await userRepository.GetByGoogleSubjectWithRolesAsync(googleSubject, cancellationToken);
            if (user is null)
            {
                var bettorRole = await roleRepository.GetByNameAsync("Bettor", cancellationToken);
                if (bettorRole is null)
                {
                    return Results.Problem(
                        title: "Authentication configuration error",
                        detail: "The Bettor role is not configured.",
                        statusCode: StatusCodes.Status500InternalServerError);
                }

                user = User.Create(googleSubject, email, displayName);
                user.UserRoles.Add(UserRole.Create(user, bettorRole));

                if (requestedRoleName == "Admin")
                {
                    var adminRole = await roleRepository.GetByNameAsync("Admin", cancellationToken);
                    if (adminRole is null)
                    {
                        return Results.Problem(
                            title: "Authentication configuration error",
                            detail: "The Admin role is not configured.",
                            statusCode: StatusCodes.Status500InternalServerError);
                    }

                    user.UserRoles.Add(UserRole.Create(user, adminRole));
                }

                await userRepository.AddAsync(user, cancellationToken);
                await userRepository.SaveChangesAsync(cancellationToken);
            }

            if (requestedRoleName == "Admin" && user.UserRoles.All(userRole => userRole.Role.Name != "Admin"))
            {
                var adminRole = await roleRepository.GetByNameAsync("Admin", cancellationToken);
                if (adminRole is null)
                {
                    return Results.Problem(
                        title: "Authentication configuration error",
                        detail: "The Admin role is not configured.",
                        statusCode: StatusCodes.Status500InternalServerError);
                }

                user.UserRoles.Add(UserRole.Create(user, adminRole));
                await userRepository.SaveChangesAsync(cancellationToken);
            }

            var roles = user.UserRoles.Select(userRole => userRole.Role.Name).Distinct(StringComparer.Ordinal).ToArray();
            var accessToken = jwtTokenGenerator.GenerateAccessToken(new AuthTokenContext(
                user.Id,
                user.Email,
                user.DisplayName,
                roles));

            return Results.Ok(new AuthResponseDto(
                accessToken,
                new AuthenticatedUserDto(user.Id, user.Email, user.DisplayName, roles)));
        })
        .WithName("DevLogin")
        .WithSummary("Development-only login shortcut for local E2E and manual testing.")
        .Produces<AuthResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        return group;
    }
}

public sealed record GoogleLoginRequest(string IdToken);

public sealed record DevLoginRequest(string? DisplayName, string? Email, string? GoogleSubject, string? Role);
