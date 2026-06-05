namespace WorldCupBets.Application.Features.Bets;

public sealed record PlaceSpecialPlayerBetResultDto(
    string Category,
    string PlayerName,
    string? ExternalPlayerId,
    decimal StakeAmountCc,
    decimal RemainingBalanceCc,
    DateTime PlacedAtUtc);
