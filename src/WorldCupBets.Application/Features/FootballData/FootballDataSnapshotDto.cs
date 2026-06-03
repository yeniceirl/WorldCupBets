namespace WorldCupBets.Application.Features.FootballData;

public sealed record FootballDataSnapshotDto(
    IReadOnlyList<FootballTeamDto> Teams,
    IReadOnlyList<FootballStadiumDto> Stadiums,
    IReadOnlyList<FootballGroupStandingDto> GroupStandings,
    IReadOnlyList<FootballMatchDto> Matches,
    DateTime? SyncedAtUtc);

public sealed record FootballTeamDto(string ExternalId, string NameEn, string FifaCode, string? Iso2, string? GroupName, string? FlagUrl);

public sealed record FootballStadiumDto(string ExternalId, string NameEn, string? FifaName, string? CityEn, string? CountryEn, int? Capacity, string? Region);

public sealed record FootballGroupStandingDto(string GroupName, string TeamExternalId, int Played, int Won, int Drawn, int Lost, int GoalsFor, int GoalsAgainst, int GoalDifference, int Points);

public sealed record FootballMatchDto(string ExternalId, string? HomeTeamExternalId, string? AwayTeamExternalId, string? HomeTeamNameEn, string? AwayTeamNameEn, string? HomeTeamLabel, string? AwayTeamLabel, string GroupName, string Matchday, string LocalDateText, string StadiumExternalId, bool IsFinished, string TimeElapsed, string StageType, int? HomeScore, int? AwayScore);
