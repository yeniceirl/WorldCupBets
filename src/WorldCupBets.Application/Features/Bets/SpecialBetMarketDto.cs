namespace WorldCupBets.Application.Features.Bets;

public sealed record SpecialBetMarketDto(
    decimal StakeAmountCc,
    DateTime? BettingClosesAtUtc,
    bool IsBettingOpen,
    IReadOnlyList<SpecialPlayerBetDto> PlayerBets);
