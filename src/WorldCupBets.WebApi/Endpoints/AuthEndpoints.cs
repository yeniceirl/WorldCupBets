using Microsoft.AspNetCore.Authorization;
using WorldCupBets.Application.Abstractions;

namespace WorldCupBets.WebApi.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/google", [AllowAnonymous] (
            GoogleLoginRequest request,
            IGoogleTokenValidator validator,
            IJwtTokenGenerator tokenGenerator) =>
        {
            _ = request;
            _ = validator;
            _ = tokenGenerator;

            return Results.Problem(
                title: "Auth scaffold placeholder",
                detail: "Google login is not implemented in this scaffold slice yet.",
                statusCode: StatusCodes.Status501NotImplemented);
        })
        .WithName("GoogleLogin")
        .WithSummary("Scaffold-only Google auth placeholder.")
        .Produces(StatusCodes.Status501NotImplemented);

        return group;
    }
}

public sealed record GoogleLoginRequest(string IdToken);
