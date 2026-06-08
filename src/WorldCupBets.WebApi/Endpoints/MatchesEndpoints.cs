using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Wolverine;
using WorldCupBets.Domain.Common;
using WorldCupBets.Domain.Entities;
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

        group.MapGet("/{id:int}/insights", async (
            int id,
            IMessageBus messageBus,
            CancellationToken cancellationToken) =>
        {
            var insights = await messageBus.InvokeAsync<MatchInsightsDto>(
                new GetMatchInsightsQuery(id),
                cancellationToken);

            return Results.Ok(insights);
        })
        .WithName("GetMatchInsights")
        .WithSummary("Get AI-generated insights (facts, head-to-head antecedents, Q&A) for a match.")
        .Produces<MatchInsightsDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        group.MapPost("/{id:int}/result", async (
            int id,
            RecordMatchResultRequest request,
            IMessageBus messageBus,
            CancellationToken cancellationToken) =>
        {
            if (!Enum.TryParse<MatchBetSelection>(request.OfficialResult, true, out var officialResult))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.OfficialResult)] = ["Official result must be one of: Home, Draw, Away."]
                });
            }

            var result = await messageBus.InvokeAsync<Result<RecordMatchResultDto>>(
                new RecordMatchResultCommand(id, officialResult),
                cancellationToken);

            if (result.IsFailure)
            {
                return result.Error?.Code switch
                {
                    "matches.not_found" => Results.NotFound(),
                    "matches.result_window_open" => Results.BadRequest(new { error = result.Error.Message }),
                    "matches.result_already_settled" => Results.Conflict(new { error = result.Error.Message }),
                    _ => Results.BadRequest(new { error = result.Error?.Message ?? "The match result could not be recorded." })
                };
            }

            return Results.Ok(result.Value);
        })
        .RequireAuthorization("Admin")
        .WithName("RecordMatchResult")
        .WithSummary("Record the official result for a match and settle match bets.")
        .Produces<RecordMatchResultDto>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);

        return group;
    }
}

public sealed record RecordMatchResultRequest(string OfficialResult);
