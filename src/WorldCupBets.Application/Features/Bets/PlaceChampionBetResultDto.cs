namespace WorldCupBets.Application.Features.Bets;

public sealed record PlaceChampionBetResultDto(
    string TeamName,
    int StakeAmountCc,
    int RemainingBalanceCc,
    DateTime PlacedAtUtc);
