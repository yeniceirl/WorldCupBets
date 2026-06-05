using Microsoft.AspNetCore.Authorization;
using Wolverine;
using WorldCupBets.Application.Abstractions;
using WorldCupBets.Application.Features.FootballData;

namespace WorldCupBets.WebApi.Endpoints;

public static class FootballDataEndpoints
{
    public static RouteGroupBuilder MapFootballDataEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/football-data")
            .WithTags("Football Data")
            .RequireAuthorization("Bettor");

        group.MapGet("/snapshot", async (IMessageBus messageBus, CancellationToken cancellationToken) =>
        {
            var result = await messageBus.InvokeAsync<FootballDataSnapshotDto>(new GetFootballDataSnapshotQuery(), cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetFootballDataSnapshot")
        .WithSummary("Get the latest cached external football data snapshot.")
        .Produces<FootballDataSnapshotDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("/players/search", async (string query, IMessageBus messageBus, CancellationToken cancellationToken) =>
        {
            if (query.Trim().Length < 3)
            {
                return Results.Ok(Array.Empty<PlayerSearchResultDto>());
            }

            var result = await messageBus.InvokeAsync<IReadOnlyList<PlayerSearchResultDto>>(new SearchPlayersQuery(query), cancellationToken);
            return Results.Ok(result);
        })
        .WithName("SearchPlayers")
        .WithSummary("Search soccer players from the external provider for tournament special bet autocomplete.")
        .Produces<IReadOnlyList<PlayerSearchResultDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        group.MapPost("/sync", [Authorize(Policy = "Admin")] async (IMessageBus messageBus, CancellationToken cancellationToken) =>
        {
            var result = await messageBus.InvokeAsync<SyncFootballDataResultDto>(new SyncFootballDataCommand(), cancellationToken);
            return Results.Ok(result);
        })
        .WithName("SyncFootballData")
        .WithSummary("Synchronize external football data into the local cache snapshot.")
        .Produces<SyncFootballDataResultDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        group.MapPost("/fixtures/group-stage/import", [Authorize(Policy = "Admin")] async (IMessageBus messageBus, CancellationToken cancellationToken) =>
        {
            var result = await messageBus.InvokeAsync<ImportGroupStageFixturesResultDto>(new ImportGroupStageFixturesCommand(), cancellationToken);
            return Results.Ok(result);
        })
        .WithName("ImportGroupStageFixtures")
        .WithSummary("Import cached external group stage fixtures into the CopaCoin match schedule.")
        .Produces<ImportGroupStageFixturesResultDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        return group;
    }
}
