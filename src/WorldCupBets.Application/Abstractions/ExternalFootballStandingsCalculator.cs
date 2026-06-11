namespace WorldCupBets.Application.Abstractions;

public static class ExternalFootballStandingsCalculator
{
    public static IReadOnlyList<ExternalFootballGroupStandingDto> Calculate(
        IReadOnlyList<ExternalFootballTeamDto> teams,
        IReadOnlyList<ExternalFootballMatchDto> matches)
    {
        var rowsByTeamId = teams
            .Where(team => !string.IsNullOrWhiteSpace(team.ExternalId) && !string.IsNullOrWhiteSpace(team.GroupName))
            .ToDictionary(
                team => team.ExternalId,
                team => new StandingAccumulator(team.GroupName!),
                StringComparer.OrdinalIgnoreCase);

        foreach (var match in matches.Where(IsFinishedGroupMatch))
        {
            if (match.HomeTeamExternalId is null
                || match.AwayTeamExternalId is null
                || !rowsByTeamId.TryGetValue(match.HomeTeamExternalId, out var home)
                || !rowsByTeamId.TryGetValue(match.AwayTeamExternalId, out var away)
                || match.HomeScore is null
                || match.AwayScore is null)
            {
                continue;
            }

            home.Played++;
            away.Played++;
            home.GoalsFor += match.HomeScore.Value;
            home.GoalsAgainst += match.AwayScore.Value;
            away.GoalsFor += match.AwayScore.Value;
            away.GoalsAgainst += match.HomeScore.Value;

            if (match.HomeScore > match.AwayScore)
            {
                home.Won++;
                away.Lost++;
                home.Points += 3;
            }
            else if (match.HomeScore < match.AwayScore)
            {
                away.Won++;
                home.Lost++;
                away.Points += 3;
            }
            else
            {
                home.Drawn++;
                away.Drawn++;
                home.Points++;
                away.Points++;
            }
        }

        return rowsByTeamId
            .Select(pair => new ExternalFootballGroupStandingDto(
                pair.Value.GroupName,
                pair.Key,
                pair.Value.Played,
                pair.Value.Won,
                pair.Value.Drawn,
                pair.Value.Lost,
                pair.Value.GoalsFor,
                pair.Value.GoalsAgainst,
                pair.Value.GoalDifference,
                pair.Value.Points))
            .ToArray();
    }

    private static bool IsFinishedGroupMatch(ExternalFootballMatchDto match)
    {
        return string.Equals(match.StageType, "group", StringComparison.OrdinalIgnoreCase)
            && match.IsFinished;
    }

    private sealed class StandingAccumulator(string groupName)
    {
        public string GroupName { get; } = groupName;
        public int Played { get; set; }
        public int Won { get; set; }
        public int Drawn { get; set; }
        public int Lost { get; set; }
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
        public int GoalDifference => GoalsFor - GoalsAgainst;
        public int Points { get; set; }
    }
}
