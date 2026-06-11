namespace WorldCupBets.Application.Abstractions;

public interface IPlayerSearchProvider
{
    Task<IReadOnlyList<PlayerSearchResultDto>> SearchAsync(string query, CancellationToken cancellationToken = default);
}
