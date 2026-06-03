namespace WorldCupBets.Application.Features.FootballData;

public sealed record SyncFootballDataResultDto(
    string ProviderName,
    int TeamsCount,
    int StadiumsCount,
    int GroupsCount,
    int MatchesCount,
    DateTime SyncedAtUtc);
