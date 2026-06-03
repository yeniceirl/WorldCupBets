using WorldCupBets.Application.Abstractions;

namespace WorldCupBets.Application.Features.FootballData;

public sealed class GetFootballDataSnapshotHandler
{
    private const string ProviderName = "worldcup26";

    public static async Task<FootballDataSnapshotDto> Handle(
        GetFootballDataSnapshotQuery query,
        IExternalFootballDataRepository externalFootballDataRepository,
        CancellationToken cancellationToken)
    {
        var snapshot = await externalFootballDataRepository.GetSnapshotAsync(ProviderName, cancellationToken);
        if (snapshot is null)
        {
            return new FootballDataSnapshotDto([], [], [], [], null);
        }

        return new FootballDataSnapshotDto(
            snapshot.Teams.Select(team => new FootballTeamDto(team.ExternalId, team.NameEn, team.FifaCode, team.Iso2, team.GroupName, team.FlagUrl)).ToArray(),
            snapshot.Stadiums.Select(stadium => new FootballStadiumDto(stadium.ExternalId, stadium.NameEn, stadium.FifaName, stadium.CityEn, stadium.CountryEn, stadium.Capacity, stadium.Region)).ToArray(),
            snapshot.GroupStandings.Select(standing => new FootballGroupStandingDto(standing.GroupName, standing.TeamExternalId, standing.Played, standing.Won, standing.Drawn, standing.Lost, standing.GoalsFor, standing.GoalsAgainst, standing.GoalDifference, standing.Points)).ToArray(),
            snapshot.Matches.Select(match => new FootballMatchDto(match.ExternalId, match.HomeTeamExternalId, match.AwayTeamExternalId, match.HomeTeamNameEn, match.AwayTeamNameEn, match.HomeTeamLabel, match.AwayTeamLabel, match.GroupName, match.Matchday, match.LocalDateText, match.StadiumExternalId, match.IsFinished, match.TimeElapsed, match.StageType, match.HomeScore, match.AwayScore)).ToArray(),
            snapshot.SyncedAtUtc);
    }
}
