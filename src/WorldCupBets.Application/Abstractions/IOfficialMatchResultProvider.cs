namespace WorldCupBets.Application.Abstractions;

public interface IOfficialMatchResultProvider
{
    string ProviderName { get; }

    Task<OfficialMatchResultConfirmation?> TryConfirmAsync(OfficialMatchResultLookup lookup, CancellationToken cancellationToken = default);
}

public sealed record OfficialMatchResultLookup(
    string HomeTeamName,
    string AwayTeamName,
    DateTime StartsAtUtc,
    DateOnly ScheduledDate);

public sealed record OfficialMatchResultConfirmation(
    string SourceReference,
    DateTime SourceUpdatedAtUtc,
    int HomeScore,
    int AwayScore);
