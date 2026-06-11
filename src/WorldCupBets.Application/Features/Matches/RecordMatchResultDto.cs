namespace WorldCupBets.Application.Features.Matches;

public sealed record RecordMatchResultDto(
    int MatchId,
    string OfficialResult,
    bool WasAlreadySettled,
    int WinnersCount,
    int LosersCount,
    decimal ChampionJackpotContributionCc,
    decimal ChampionJackpotCc,
    DateTime SettledAtUtc);
