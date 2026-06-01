using System.Security.Claims;
using Wolverine;
using WorldCupBets.Application.Features.Users;
using WorldCupBets.Domain.Common;

namespace WorldCupBets.WebApi.Endpoints;

public static class MeEndpoints
{
    public static RouteGroupBuilder MapMeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/me")
            .WithTags("Me")
            .RequireAuthorization("Bettor");

        group.MapGet("/summary", async (
            ClaimsPrincipal user,
            IMessageBus messageBus,
            CancellationToken cancellationToken) =>
        {
            if (!int.TryParse(user.FindFirstValue("sub"), out var userId))
            {
                return Results.Unauthorized();
            }

            var result = await messageBus.InvokeAsync<Result<CurrentUserSummaryDto>>(
                new GetCurrentUserSummaryQuery(userId),
                cancellationToken);

            if (result.IsFailure)
            {
                return result.Error?.Code switch
                {
                    "users.user_not_found" => Results.Unauthorized(),
                    _ => Results.BadRequest(new { error = result.Error?.Message ?? "Unable to load the current user summary." })
                };
            }

            return Results.Ok(result.Value);
        })
        .WithName("GetCurrentUserSummary")
        .WithSummary("Get the authenticated bettor wallet summary and rescue state.")
        .Produces<CurrentUserSummaryDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);

        return group;
    }
}
