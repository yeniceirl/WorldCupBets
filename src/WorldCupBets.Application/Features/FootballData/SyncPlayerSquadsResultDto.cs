namespace WorldCupBets.Application.Features.FootballData;

public sealed record SyncPlayerSquadsResultDto(
    string ProviderName,
    bool NotConfigured,
    int TeamsProcessedCount,
    int PlayersIndexedCount,
    IReadOnlyList<PlayerSquadSyncErrorDto> Errors,
    DateTime SyncedAtUtc);

public sealed record PlayerSquadSyncErrorDto(string TeamName, string Message, bool RateLimited);
