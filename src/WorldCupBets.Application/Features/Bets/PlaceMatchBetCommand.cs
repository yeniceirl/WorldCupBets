using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Application.Features.Bets;

public sealed record PlaceMatchBetCommand(
    int UserId,
    int MatchId,
    MatchBetSelection Selection);
