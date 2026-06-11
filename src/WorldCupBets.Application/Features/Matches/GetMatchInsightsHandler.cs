using WorldCupBets.Application.Abstractions;
using WorldCupBets.Domain.Repositories;

namespace WorldCupBets.Application.Features.Matches;

public sealed class GetMatchInsightsHandler
{
    private const string FootballDataProviderName = "worldcup26";

    public static async Task<MatchInsightsDto> Handle(
        GetMatchInsightsQuery query,
        IMatchRepository matchRepository,
        IExternalFootballDataRepository externalFootballDataRepository,
        IAiInsightsProvider aiInsightsProvider,
        CancellationToken cancellationToken)
    {
        var match = await matchRepository.GetByIdAsync(query.MatchId, cancellationToken);
        if (match is null)
        {
            return MatchInsightsDto.Unavailable;
        }

        var snapshot = await externalFootballDataRepository.GetSnapshotAsync(FootballDataProviderName, cancellationToken);

        var prompt = new MatchInsightsPrompt(
            match.HomeTeamName,
            match.AwayTeamName,
            match.GetStageLabel(),
            match.GroupName,
            match.StartsAtUtc,
            match.Venue,
            BuildGroupStandings(snapshot, match.HomeTeamName),
            BuildGroupStandings(snapshot, match.AwayTeamName));

        var result = await aiInsightsProvider.GenerateAsync(prompt, cancellationToken);
        if (!result.IsAvailable)
        {
            return MatchInsightsDto.Unavailable;
        }

        return new MatchInsightsDto(
            true,
            result.Facts.Select(fact => new InsightFactDto(fact.Text)).ToArray(),
            result.Antecedents.Select(antecedent => new InsightAntecedentDto(antecedent.Text)).ToArray(),
            result.Qa.Select(qa => new InsightQaDto(qa.Question, qa.Answer)).ToArray());
    }

    private static IReadOnlyList<GroupStandingRow> BuildGroupStandings(ExternalFootballSnapshot? snapshot, string teamName)
    {
        if (snapshot is null)
        {
            return [];
        }

        var team = snapshot.Teams.FirstOrDefault(candidate =>
            string.Equals(candidate.NameEn, teamName, StringComparison.OrdinalIgnoreCase));
        if (team is null)
        {
            return [];
        }

        var computedGroupStandings = ExternalFootballStandingsCalculator.Calculate(snapshot.Teams, snapshot.Matches);

        var groupStandings = computedGroupStandings
            .Where(standing => string.Equals(standing.GroupName, team.GroupName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (groupStandings.Length == 0)
        {
            return [];
        }

        var teamsByExternalId = snapshot.Teams.ToDictionary(candidate => candidate.ExternalId, candidate => candidate.NameEn);

        return groupStandings
            .Select(standing => new GroupStandingRow(
                teamsByExternalId.GetValueOrDefault(standing.TeamExternalId, standing.TeamExternalId),
                standing.Played,
                standing.Won,
                standing.Drawn,
                standing.Lost,
                standing.GoalsFor,
                standing.GoalsAgainst,
                standing.GoalDifference,
                standing.Points))
            .ToArray();
    }
}
