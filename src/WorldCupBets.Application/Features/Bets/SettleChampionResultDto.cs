namespace WorldCupBets.Application.Features.Bets;

public sealed record SettleChampionResultDto(
    string ChampionTeamName,
    bool WasAlreadySettled,
    int WinnersCount,
    int LosersCount,
    int ChampionJackpotCc,
    int LosingStakePoolCc,
    int ProfitSharePerWinnerCc,
    int TotalPayoutPerWinnerCc,
    int UndistributedJackpotCc,
    DateTime SettledAtUtc);
