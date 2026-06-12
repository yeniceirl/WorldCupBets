namespace WorldCupBets.Application.Features.Matches;

public sealed record MatchResultSyncOptions(
    bool Enabled,
    int PollIntervalMinutes);
