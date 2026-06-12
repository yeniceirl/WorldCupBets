namespace WorldCupBets.Application.Features.Matches;

public sealed record SyncMatchResultsCommand(
    int? MatchId = null,
    bool Force = false,
    int? MaxMatches = null);
