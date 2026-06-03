using Microsoft.EntityFrameworkCore;
using WorldCupBets.Application.Abstractions;
using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Infrastructure.Persistence.Repositories;

public sealed class ExternalFootballDataRepository(AppDbContext dbContext) : IExternalFootballDataRepository
{
    public async Task ReplaceSnapshotAsync(string providerName, ExternalFootballSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        var existingTeams = await dbContext.ExternalFootballTeams.Where(team => team.ProviderName == providerName).ToArrayAsync(cancellationToken);
        var existingStadiums = await dbContext.ExternalFootballStadiums.Where(stadium => stadium.ProviderName == providerName).ToArrayAsync(cancellationToken);
        var existingStandings = await dbContext.ExternalFootballGroupStandings.Where(standing => standing.ProviderName == providerName).ToArrayAsync(cancellationToken);
        var existingMatches = await dbContext.ExternalFootballMatches.Where(match => match.ProviderName == providerName).ToArrayAsync(cancellationToken);

        dbContext.ExternalFootballTeams.RemoveRange(existingTeams);
        dbContext.ExternalFootballStadiums.RemoveRange(existingStadiums);
        dbContext.ExternalFootballGroupStandings.RemoveRange(existingStandings);
        dbContext.ExternalFootballMatches.RemoveRange(existingMatches);

        dbContext.ExternalFootballTeams.AddRange(snapshot.Teams.Select(team => ExternalFootballTeam.Create(providerName, team.ExternalId, team.NameEn, team.FifaCode, team.Iso2, team.GroupName, team.FlagUrl, snapshot.SyncedAtUtc)));
        dbContext.ExternalFootballStadiums.AddRange(snapshot.Stadiums.Select(stadium => ExternalFootballStadium.Create(providerName, stadium.ExternalId, stadium.NameEn, stadium.FifaName, stadium.CityEn, stadium.CountryEn, stadium.Capacity, stadium.Region, snapshot.SyncedAtUtc)));
        dbContext.ExternalFootballGroupStandings.AddRange(snapshot.GroupStandings.Select(standing => ExternalFootballGroupStanding.Create(providerName, standing.GroupName, standing.TeamExternalId, standing.Played, standing.Won, standing.Drawn, standing.Lost, standing.GoalsFor, standing.GoalsAgainst, standing.GoalDifference, standing.Points, snapshot.SyncedAtUtc)));
        dbContext.ExternalFootballMatches.AddRange(snapshot.Matches.Select(match => ExternalFootballMatch.Create(providerName, match.ExternalId, match.HomeTeamExternalId, match.AwayTeamExternalId, match.HomeTeamNameEn, match.AwayTeamNameEn, match.HomeTeamLabel, match.AwayTeamLabel, match.GroupName, match.Matchday, match.LocalDateText, match.StadiumExternalId, match.IsFinished, match.TimeElapsed, match.StageType, match.HomeScore, match.AwayScore, snapshot.SyncedAtUtc)));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ExternalFootballSnapshot?> GetSnapshotAsync(string providerName, CancellationToken cancellationToken = default)
    {
        var teams = await dbContext.ExternalFootballTeams.AsNoTracking().Where(team => team.ProviderName == providerName).OrderBy(team => team.NameEn).ToArrayAsync(cancellationToken);
        if (teams.Length == 0)
        {
            return null;
        }

        var stadiums = await dbContext.ExternalFootballStadiums.AsNoTracking().Where(stadium => stadium.ProviderName == providerName).OrderBy(stadium => stadium.NameEn).ToArrayAsync(cancellationToken);
        var standings = await dbContext.ExternalFootballGroupStandings.AsNoTracking().Where(standing => standing.ProviderName == providerName).OrderBy(standing => standing.GroupName).ThenByDescending(standing => standing.Points).ToArrayAsync(cancellationToken);
        var matches = (await dbContext.ExternalFootballMatches.AsNoTracking().Where(match => match.ProviderName == providerName).ToArrayAsync(cancellationToken))
            .OrderBy(match => int.TryParse(match.ExternalId, out var externalId) ? externalId : int.MaxValue)
            .ToArray();
        var syncedAtUtc = new[] { teams.Select(team => team.SyncedAtUtc), stadiums.Select(stadium => stadium.SyncedAtUtc), standings.Select(standing => standing.SyncedAtUtc), matches.Select(match => match.SyncedAtUtc) }
            .SelectMany(values => values)
            .DefaultIfEmpty()
            .Max();

        return new ExternalFootballSnapshot(
            teams.Select(team => new ExternalFootballTeamDto(team.ExternalId, team.NameEn, team.FifaCode, team.Iso2, team.GroupName, team.FlagUrl)).ToArray(),
            stadiums.Select(stadium => new ExternalFootballStadiumDto(stadium.ExternalId, stadium.NameEn, stadium.FifaName, stadium.CityEn, stadium.CountryEn, stadium.Capacity, stadium.Region)).ToArray(),
            standings.Select(standing => new ExternalFootballGroupStandingDto(standing.GroupName, standing.TeamExternalId, standing.Played, standing.Won, standing.Drawn, standing.Lost, standing.GoalsFor, standing.GoalsAgainst, standing.GoalDifference, standing.Points)).ToArray(),
            matches.Select(match => new ExternalFootballMatchDto(match.ExternalId, match.HomeTeamExternalId, match.AwayTeamExternalId, match.HomeTeamNameEn, match.AwayTeamNameEn, match.HomeTeamLabel, match.AwayTeamLabel, match.GroupName, match.Matchday, match.LocalDateText, match.StadiumExternalId, match.IsFinished, match.TimeElapsed, match.StageType, match.HomeScore, match.AwayScore)).ToArray(),
            syncedAtUtc);
    }
}
