using Wolverine;
using WorldCupBets.Application.Features.Leaderboard;

namespace WorldCupBets.WebApi.Endpoints;

public static class LeaderboardEndpoints
{
    public static RouteGroupBuilder MapLeaderboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/leaderboard")
            .WithTags("Leaderboard")
            .RequireAuthorization("Bettor");

        group.MapGet(string.Empty, async (
            IMessageBus messageBus,
            CancellationToken cancellationToken) =>
        {
            var leaderboard = await messageBus.InvokeAsync<IReadOnlyList<LeaderboardItemDto>>(
                new GetLeaderboardQuery(),
                cancellationToken);

            return Results.Ok(leaderboard);
        })
        .WithName("GetLeaderboard")
        .WithSummary("List bettors ordered by current CopaCoin balance.")
        .Produces<IReadOnlyList<LeaderboardItemDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        return group;
    }
}
