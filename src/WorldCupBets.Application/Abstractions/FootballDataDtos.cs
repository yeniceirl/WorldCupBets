namespace WorldCupBets.Application.Abstractions;

public sealed record ExternalFootballSnapshot(
    IReadOnlyList<ExternalFootballTeamDto> Teams,
    IReadOnlyList<ExternalFootballStadiumDto> Stadiums,
    IReadOnlyList<ExternalFootballGroupStandingDto> GroupStandings,
    IReadOnlyList<ExternalFootballMatchDto> Matches,
    DateTime SyncedAtUtc);

public sealed record ExternalFootballTeamDto(
    string ExternalId,
    string NameEn,
    string FifaCode,
    string? Iso2,
    string? GroupName,
    string? FlagUrl);

public sealed record ExternalFootballStadiumDto(
    string ExternalId,
    string NameEn,
    string? FifaName,
    string? CityEn,
    string? CountryEn,
    int? Capacity,
    string? Region);

public sealed record ExternalFootballGroupStandingDto(
    string GroupName,
    string TeamExternalId,
    int Played,
    int Won,
    int Drawn,
    int Lost,
    int GoalsFor,
    int GoalsAgainst,
    int GoalDifference,
    int Points);

public sealed record ExternalFootballMatchDto(
    string ExternalId,
    string? HomeTeamExternalId,
    string? AwayTeamExternalId,
    string? HomeTeamNameEn,
    string? AwayTeamNameEn,
    string? HomeTeamLabel,
    string? AwayTeamLabel,
    string GroupName,
    string Matchday,
    string LocalDateText,
    string StadiumExternalId,
    bool IsFinished,
    string TimeElapsed,
    string StageType,
    int? HomeScore,
    int? AwayScore);
