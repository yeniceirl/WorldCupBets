using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Wolverine;
using WorldCupBets.Application.Features.Matches;

namespace WorldCupBets.WebApi.Endpoints;

public static class MatchesEndpoints
{
    public static RouteGroupBuilder MapMatchesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/matches")
            .WithTags("Matches")
            .RequireAuthorization("Bettor");

        group.MapGet(string.Empty, async (
            ClaimsPrincipal user,
            IMessageBus messageBus,
            CancellationToken cancellationToken) =>
        {
            var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("sub");

            if (!int.TryParse(userIdValue, out var userId))
            {
                return Results.Unauthorized();
            }

            var matches = await messageBus.InvokeAsync<IReadOnlyList<MatchListItemDto>>(
                new GetMatchesQuery(userId),
                cancellationToken);

            return Results.Ok(matches);
        })
        .WithName("ListMatches")
        .WithSummary("List the current demo match schedule for authenticated bettors.")
        .Produces<IReadOnlyList<MatchListItemDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        return group;
    }
}
