using System.Security.Claims;
using Wolverine;
using WorldCupBets.Application.Features.Bets;
using WorldCupBets.Domain.Common;
using WorldCupBets.Domain.Entities;

namespace WorldCupBets.WebApi.Endpoints;

public static class BetsEndpoints
{
    public static RouteGroupBuilder MapBetsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/bets")
            .WithTags("Bets")
            .RequireAuthorization("Bettor");

        group.MapPost("/matches", async (
            PlaceMatchBetRequest request,
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

            if (!Enum.TryParse<MatchBetSelection>(request.Selection, true, out var selection))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.Selection)] = ["Selection must be one of: Home, Draw, Away."]
                });
            }

            var result = await messageBus.InvokeAsync<Result<PlaceMatchBetResultDto>>(
                new PlaceMatchBetCommand(userId, request.MatchId, selection),
                cancellationToken);

            if (result.IsFailure)
            {
                return result.Error?.Code switch
                {
                    "bets.match_not_found" => Results.NotFound(),
                    "bets.match_bet_already_exists" => Results.Conflict(new { error = result.Error.Message }),
                    "bets.match_betting_closed" => Results.BadRequest(new { error = result.Error.Message }),
                    "bets.insufficient_balance" => Results.BadRequest(new { error = result.Error.Message }),
                    "bets.user_not_found" => Results.Unauthorized(),
                    _ => Results.BadRequest(new { error = result.Error?.Message ?? "The match bet could not be placed." })
                };
            }

            return Results.Ok(result.Value);
        })
        .WithName("PlaceMatchBet")
        .WithSummary("Place a winner bet for a match as the authenticated bettor.")
        .Produces<PlaceMatchBetResultDto>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);

        group.MapGet("/champion", async (
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

            var result = await messageBus.InvokeAsync<ChampionBetMarketDto>(
                new GetChampionBetMarketQuery(userId),
                cancellationToken);

            return Results.Ok(result);
        })
        .WithName("GetChampionBetMarket")
        .WithSummary("Get champion bet options, closing metadata, and the current bettor selection.")
        .Produces<ChampionBetMarketDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/champion", async (
            PlaceChampionBetRequest request,
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

            if (string.IsNullOrWhiteSpace(request.TeamName))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.TeamName)] = ["Team name is required."]
                });
            }

            var result = await messageBus.InvokeAsync<Result<PlaceChampionBetResultDto>>(
                new PlaceChampionBetCommand(userId, request.TeamName.Trim()),
                cancellationToken);

            if (result.IsFailure)
            {
                return result.Error?.Code switch
                {
                    "bets.invalid_champion_team" => Results.BadRequest(new { error = result.Error.Message }),
                    "bets.champion_betting_closed" => Results.BadRequest(new { error = result.Error.Message }),
                    "bets.insufficient_balance" => Results.BadRequest(new { error = result.Error.Message }),
                    "bets.champion_bet_already_exists" => Results.Conflict(new { error = result.Error.Message }),
                    "bets.user_not_found" => Results.Unauthorized(),
                    _ => Results.BadRequest(new { error = result.Error?.Message ?? "The champion bet could not be placed." })
                };
            }

            return Results.Ok(result.Value);
        })
        .WithName("PlaceChampionBet")
        .WithSummary("Place a champion bet as the authenticated bettor.")
        .Produces<PlaceChampionBetResultDto>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/champion/settlement", async (
            SettleChampionRequest request,
            IMessageBus messageBus,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.ChampionTeamName))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.ChampionTeamName)] = ["Champion team name is required."]
                });
            }

            var result = await messageBus.InvokeAsync<Result<SettleChampionResultDto>>(
                new SettleChampionCommand(request.ChampionTeamName.Trim()),
                cancellationToken);

            if (result.IsFailure)
            {
                return result.Error?.Code switch
                {
                    "bets.champion_required" => Results.BadRequest(new { error = result.Error.Message }),
                    "bets.champion_already_settled" => Results.Conflict(new { error = result.Error.Message }),
                    _ => Results.BadRequest(new { error = result.Error?.Message ?? "Champion settlement could not be completed." })
                };
            }

            return Results.Ok(result.Value);
        })
        .RequireAuthorization("Admin")
        .WithName("SettleChampion")
        .WithSummary("Settle champion bets and record any undistributed jackpot remainder.")
        .Produces<SettleChampionResultDto>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status409Conflict);

        group.MapGet("/special", async (
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

            var result = await messageBus.InvokeAsync<SpecialBetMarketDto>(new GetSpecialBetMarketQuery(userId), cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetSpecialBetMarket")
        .WithSummary("Get tournament special bet metadata and current bettor player selections.")
        .Produces<SpecialBetMarketDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/special/player", async (
            PlaceSpecialPlayerBetRequest request,
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

            if (!Enum.TryParse<TournamentPickCategory>(request.Category, true, out var category)
                || category is TournamentPickCategory.Champion)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.Category)] = ["Category must be one of: BestPlayer, TopScorer."]
                });
            }

            if (string.IsNullOrWhiteSpace(request.PlayerName))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.PlayerName)] = ["Player name is required."]
                });
            }

            var result = await messageBus.InvokeAsync<Result<PlaceSpecialPlayerBetResultDto>>(
                new PlaceSpecialPlayerBetCommand(userId, category, request.PlayerName, request.ExternalPlayerId),
                cancellationToken);

            if (result.IsFailure)
            {
                return result.Error?.Code switch
                {
                    "bets.invalid_player_name" => Results.BadRequest(new { error = result.Error.Message }),
                    "bets.special_betting_closed" => Results.BadRequest(new { error = result.Error.Message }),
                    "bets.insufficient_balance" => Results.BadRequest(new { error = result.Error.Message }),
                    "bets.special_player_bet_already_exists" => Results.Conflict(new { error = result.Error.Message }),
                    "bets.user_not_found" => Results.Unauthorized(),
                    _ => Results.BadRequest(new { error = result.Error?.Message ?? "The player bet could not be placed." })
                };
            }

            return Results.Ok(result.Value);
        })
        .WithName("PlaceSpecialPlayerBet")
        .WithSummary("Place a player-based tournament special bet as the authenticated bettor.")
        .Produces<PlaceSpecialPlayerBetResultDto>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status409Conflict);

        return group;
    }
}

public sealed record PlaceMatchBetRequest(int MatchId, string Selection);

public sealed record PlaceChampionBetRequest(string TeamName);

public sealed record SettleChampionRequest(string ChampionTeamName);

public sealed record PlaceSpecialPlayerBetRequest(string Category, string PlayerName, string? ExternalPlayerId);
