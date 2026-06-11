namespace WorldCupBets.Application.Features.Bets;

public sealed record SettleChampionResultDto(
    string ChampionTeamName,
    bool WasAlreadySettled,
    int WinnersCount,
    int LosersCount,
    decimal ChampionJackpotCc,
    decimal LosingStakePoolCc,
    decimal ProfitSharePerWinnerCc,
    decimal TotalPayoutPerWinnerCc,
    decimal UndistributedJackpotCc,
    DateTime SettledAtUtc);
