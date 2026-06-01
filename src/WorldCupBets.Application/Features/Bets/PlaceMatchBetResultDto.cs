namespace WorldCupBets.Application.Features.Bets;

public sealed record PlaceMatchBetResultDto(
    int MatchId,
    string Selection,
    int StakeAmountCc,
    int RemainingBalanceCc,
    DateTime PlacedAtUtc);
