namespace WorldCupBets.Application.Features.Bets;

public sealed record SpecialPlayerBetDto(
    string Category,
    string PlayerName,
    string? ExternalPlayerId,
    decimal StakeAmountCc,
    DateTime PlacedAtUtc);
