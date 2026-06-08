namespace WorldCupBets.Application.Abstractions;

public interface IAiInsightsProvider
{
    Task<MatchInsightsResult> GenerateAsync(MatchInsightsPrompt prompt, CancellationToken cancellationToken = default);
}

public sealed record MatchInsightsPrompt(
    string HomeTeamName,
    string AwayTeamName,
    string Stage,
    string? GroupName,
    DateTime StartsAtUtc,
    string? Venue,
    IReadOnlyList<GroupStandingRow> HomeTeamGroupStandings,
    IReadOnlyList<GroupStandingRow> AwayTeamGroupStandings);

public sealed record GroupStandingRow(
    string TeamName,
    int Played,
    int Won,
    int Drawn,
    int Lost,
    int GoalsFor,
    int GoalsAgainst,
    int GoalDifference,
    int Points);

public sealed record MatchInsightsResult(
    bool IsAvailable,
    IReadOnlyList<InsightFact> Facts,
    IReadOnlyList<InsightAntecedent> Antecedents,
    IReadOnlyList<InsightQaPair> Qa)
{
    public static MatchInsightsResult Unavailable { get; } = new(false, [], [], []);
}

public sealed record InsightFact(string Text);

public sealed record InsightAntecedent(string Text);

public sealed record InsightQaPair(string Question, string Answer);
