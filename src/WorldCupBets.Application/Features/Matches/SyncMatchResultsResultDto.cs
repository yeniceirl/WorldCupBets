namespace WorldCupBets.Application.Features.Matches;

public sealed record SyncMatchResultsResultDto(
    bool ApiSportsConfigured,
    DateTime? WorldCupSnapshotSyncedAtUtc,
    int CandidatesConsideredCount,
    int ConfirmedCount,
    int AlreadySettledCount,
    int DeferredCount,
    int FailedCount,
    IReadOnlyList<MatchResultSyncItemDto> Items);

public sealed record MatchResultSyncItemDto(
    int MatchId,
    string Status,
    string Message);
