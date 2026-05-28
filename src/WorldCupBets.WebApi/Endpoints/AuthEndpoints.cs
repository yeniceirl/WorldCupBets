using Microsoft.AspNetCore.Authorization;
using Wolverine;
using WorldCupBets.Application.Features.Auth;
using WorldCupBets.Domain.Common;

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

        return group;
    }
}

public sealed record GoogleLoginRequest(string IdToken);
