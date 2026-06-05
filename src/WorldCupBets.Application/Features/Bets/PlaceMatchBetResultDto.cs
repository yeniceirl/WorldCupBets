namespace WorldCupBets.Application.Features.Bets;

public sealed record PlaceMatchBetResultDto(
    int MatchId,
    string Selection,
    decimal StakeAmountCc,
    decimal RemainingBalanceCc,
    DateTime PlacedAtUtc);
