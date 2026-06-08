namespace WorldCupBets.Application.Features.Matches;

public sealed record MatchInsightsDto(
    bool IsAvailable,
    IReadOnlyList<InsightFactDto> Facts,
    IReadOnlyList<InsightAntecedentDto> Antecedents,
    IReadOnlyList<InsightQaDto> Qa)
{
    public static MatchInsightsDto Unavailable { get; } = new(false, [], [], []);
}

public sealed record InsightFactDto(string Text);

public sealed record InsightAntecedentDto(string Text);

public sealed record InsightQaDto(string Question, string Answer);
