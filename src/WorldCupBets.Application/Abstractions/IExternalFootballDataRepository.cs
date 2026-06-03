namespace WorldCupBets.Application.Abstractions;

public interface IExternalFootballDataRepository
{
    Task ReplaceSnapshotAsync(string providerName, ExternalFootballSnapshot snapshot, CancellationToken cancellationToken = default);

    Task<ExternalFootballSnapshot?> GetSnapshotAsync(string providerName, CancellationToken cancellationToken = default);
}
