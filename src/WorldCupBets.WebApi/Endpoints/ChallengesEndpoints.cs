using System.Security.Claims;
using Wolverine;
using WorldCupBets.Application.Features.Challenges;
using WorldCupBets.Domain.Common;
using WorldCupBets.Domain.Entities;

namespace WorldCupBets.WebApi.Endpoints;

public static class ChallengesEndpoints
{
    public static RouteGroupBuilder MapChallengesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/challenges")
            .WithTags("Challenges")
            .RequireAuthorization("Bettor");

        group.MapGet(string.Empty, async (int? matchId, IMessageBus messageBus, CancellationToken cancellationToken) =>
        {
            if (matchId is null or <= 0)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(matchId)] = ["A valid matchId query parameter is required."]
                });
            }

            var challenges = await messageBus.InvokeAsync<IReadOnlyList<ChallengeDto>>(new ListChallengesQuery(matchId.Value), cancellationToken);
            return Results.Ok(challenges);
        })
        .WithName("ListChallenges")
        .WithSummary("List custom challenges for a match.")
        .Produces<IReadOnlyList<ChallengeDto>>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        group.MapPost(string.Empty, async (
            CreateChallengeRequest request,
            ClaimsPrincipal user,
            IMessageBus messageBus,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(user, out var userId))
            {
                return Results.Unauthorized();
            }

            var result = await messageBus.InvokeAsync<Result<ChallengeMutationResultDto>>(
                new CreateChallengeCommand(userId, request.MatchId, request.ClaimText, request.CreatorSideText, request.TakerSideText, request.StakeAmountCc),
                cancellationToken);

            return ToCreateOrAcceptResponse(result, "The challenge could not be created.");
        })
        .WithName("CreateChallenge")
        .WithSummary("Create an open custom challenge and escrow the creator stake.")
        .Produces<ChallengeMutationResultDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/{id:int}/accept", async (
            int id,
            ClaimsPrincipal user,
            IMessageBus messageBus,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(user, out var userId))
            {
                return Results.Unauthorized();
            }

            var result = await messageBus.InvokeAsync<Result<ChallengeMutationResultDto>>(new AcceptChallengeCommand(id, userId), cancellationToken);
            return ToCreateOrAcceptResponse(result, "The challenge could not be accepted.");
        })
        .WithName("AcceptChallenge")
        .WithSummary("Accept an open custom challenge and escrow the taker stake.")
        .Produces<ChallengeMutationResultDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/{id:int}/cancel", async (
            int id,
            ClaimsPrincipal user,
            IMessageBus messageBus,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(user, out var userId))
            {
                return Results.Unauthorized();
            }

            var result = await messageBus.InvokeAsync<Result<ChallengeMutationResultDto>>(new CancelChallengeCommand(id, userId), cancellationToken);
            return ToCreateOrAcceptResponse(result, "The challenge could not be canceled.");
        })
        .WithName("CancelChallenge")
        .WithSummary("Cancel an open custom challenge created by the current user and refund the creator stake.")
        .Produces<ChallengeMutationResultDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/{id:int}/settlement", async (
            int id,
            SettleChallengeRequest request,
            IMessageBus messageBus,
            CancellationToken cancellationToken) =>
        {
            if (!Enum.TryParse<MatchChallengeSide>(request.WinnerSide, true, out var winnerSide) || !Enum.IsDefined(winnerSide))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.WinnerSide)] = ["WinnerSide must be one of: Creator, Taker."]
                });
            }

            var result = await messageBus.InvokeAsync<Result<ChallengeDto>>(new SettleChallengeCommand(id, winnerSide), cancellationToken);
            return ToLifecycleResponse(result, "The challenge could not be settled.");
        })
        .RequireAuthorization("Admin")
        .WithName("SettleChallenge")
        .WithSummary("Settle a matched custom challenge for the selected side.")
        .Produces<ChallengeDto>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/{id:int}/void", async (int id, IMessageBus messageBus, CancellationToken cancellationToken) =>
        {
            var result = await messageBus.InvokeAsync<Result<ChallengeDto>>(new VoidChallengeCommand(id), cancellationToken);
            return ToLifecycleResponse(result, "The challenge could not be voided.");
        })
        .RequireAuthorization("Admin")
        .WithName("VoidChallenge")
        .WithSummary("Void an active custom challenge and refund current participants.")
        .Produces<ChallengeDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/{id:int}/expire", async (int id, IMessageBus messageBus, CancellationToken cancellationToken) =>
        {
            var result = await messageBus.InvokeAsync<Result<ChallengeDto>>(new ExpireChallengeCommand(id), cancellationToken);
            return ToLifecycleResponse(result, "The challenge could not be expired.");
        })
        .RequireAuthorization("Admin")
        .WithName("ExpireChallenge")
        .WithSummary("Expire an active custom challenge and refund current participants.")
        .Produces<ChallengeDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);

        return group;
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out int userId)
    {
        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return int.TryParse(userIdValue, out userId);
    }

    private static IResult ToCreateOrAcceptResponse(Result<ChallengeMutationResultDto> result, string fallbackMessage)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        return result.Error?.Code switch
        {
            "challenges.user_not_found" => Results.Unauthorized(),
            "challenges.match_not_found" or "challenges.not_found" => Results.NotFound(),
            "challenges.not_open" or "challenges.self_accept" or "challenges.not_creator" or "challenges.window_closed" => Results.Conflict(new { error = result.Error.Message }),
            "challenges.invalid_payload" or "challenges.insufficient_balance" => Results.BadRequest(new { error = result.Error.Message }),
            _ => Results.BadRequest(new { error = result.Error?.Message ?? fallbackMessage })
        };
    }

    private static IResult ToLifecycleResponse(Result<ChallengeDto> result, string fallbackMessage)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        return result.Error?.Code switch
        {
            "challenges.not_found" => Results.NotFound(),
            "challenges.not_matched" or "challenges.terminal" => Results.Conflict(new { error = result.Error.Message }),
            "challenges.invalid_payload" => Results.BadRequest(new { error = result.Error.Message }),
            _ => Results.BadRequest(new { error = result.Error?.Message ?? fallbackMessage })
        };
    }
}

public sealed record CreateChallengeRequest(
    int MatchId,
    string ClaimText,
    string CreatorSideText,
    string TakerSideText,
    decimal StakeAmountCc);

public sealed record SettleChallengeRequest(string WinnerSide);
