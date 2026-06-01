namespace WorldCupBets.Application.Features.Bets;

public sealed record ChampionBetMarketDto(
    IReadOnlyList<string> TeamOptions,
    int StakeAmountCc,
    DateTime? BettingClosesAtUtc,
    bool IsBettingOpen,
    string? CurrentUserChampionTeamName);
