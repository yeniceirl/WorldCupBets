using WorldCupBets.Application.Abstractions;

namespace WorldCupBets.Application.Features.FootballData;

public sealed class SyncFootballDataHandler
{
    public static async Task<SyncFootballDataResultDto> Handle(
        SyncFootballDataCommand command,
        IFootballDataProvider footballDataProvider,
        IExternalFootballDataRepository externalFootballDataRepository,
        CancellationToken cancellationToken)
    {
        var snapshot = await footballDataProvider.GetSnapshotAsync(cancellationToken);
        await externalFootballDataRepository.ReplaceSnapshotAsync(footballDataProvider.ProviderName, snapshot, cancellationToken);

        return new SyncFootballDataResultDto(
            footballDataProvider.ProviderName,
            snapshot.Teams.Count,
            snapshot.Stadiums.Count,
            snapshot.GroupStandings.Select(standing => standing.GroupName).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            snapshot.Matches.Count,
            snapshot.SyncedAtUtc);
    }
}
