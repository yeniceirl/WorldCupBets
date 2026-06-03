namespace WorldCupBets.Application.Abstractions;

public interface IFootballDataProvider
{
    string ProviderName { get; }

    Task<ExternalFootballSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}
