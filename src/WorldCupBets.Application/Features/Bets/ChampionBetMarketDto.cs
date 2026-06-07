namespace WorldCupBets.Application.Features.Bets;

public sealed record ChampionBetMarketDto(
    IReadOnlyList<string> TeamOptions,
    decimal StakeAmountCc,
    DateTime? BettingClosesAtUtc,
    bool IsBettingOpen,
    bool IsSettled,
    string? CurrentUserChampionTeamName,
    string? CurrentUserChampionTeamFlagUrl);
