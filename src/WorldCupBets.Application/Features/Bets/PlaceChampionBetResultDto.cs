namespace WorldCupBets.Application.Features.Bets;

public sealed record PlaceChampionBetResultDto(
    string TeamName,
    decimal StakeAmountCc,
    decimal RemainingBalanceCc,
    DateTime PlacedAtUtc);
