using WorldCupBets.Application.Abstractions;
using Xunit;

namespace WorldCupBets.Application.Tests;

public sealed class ExternalFootballStandingsCalculatorTests
{
    [Fact]
    public void Calculate_Uses_Finished_Group_Match_Scores_To_Build_Standings()
    {
        var teams = new[]
        {
            new ExternalFootballTeamDto("1", "Mexico", "MEX", "mx", "A", null),
            new ExternalFootballTeamDto("2", "South Africa", "RSA", "za", "A", null),
            new ExternalFootballTeamDto("3", "South Korea", "KOR", "kr", "A", null),
            new ExternalFootballTeamDto("4", "Czech Republic", "CZE", "cz", "A", null),
        };

        var matches = new[]
        {
            new ExternalFootballMatchDto("1", "1", "2", "Mexico", "South Africa", null, null, "A", "1", "06/11/2026 13:00", "1", true, "finished", "group", 2, 0),
            new ExternalFootballMatchDto("2", "3", "4", "South Korea", "Czech Republic", null, null, "A", "1", "06/11/2026 20:00", "2", false, "notstarted", "group", 0, 0),
        };

        var standings = ExternalFootballStandingsCalculator.Calculate(teams, matches)
            .OrderByDescending(row => row.Points)
            .ThenByDescending(row => row.GoalDifference)
            .ThenByDescending(row => row.GoalsFor)
            .ToArray();

        Assert.Equal("1", standings[0].TeamExternalId);
        Assert.Equal(3, standings[0].Points);
        Assert.Equal(2, standings[0].GoalsFor);
        Assert.Equal(0, standings[0].GoalsAgainst);

        Assert.Equal("2", standings[3].TeamExternalId);
        Assert.Equal(0, standings[3].Points);
        Assert.Equal(0, standings[3].GoalsFor);
        Assert.Equal(2, standings[3].GoalsAgainst);
    }
}
