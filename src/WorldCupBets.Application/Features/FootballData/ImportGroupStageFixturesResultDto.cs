namespace WorldCupBets.Application.Features.FootballData;

public sealed record ImportGroupStageFixturesResultDto(
    string ProviderName,
    int ImportedCount,
    int UpdatedCount,
    int SkippedCount,
    DateTime SourceSyncedAtUtc);
