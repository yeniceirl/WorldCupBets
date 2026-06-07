using WorldCupBets.Application.Abstractions;

namespace WorldCupBets.Infrastructure.ExternalFootball;

public sealed class EmptyPlayerSearchProvider : IPlayerSearchProvider
{
    public Task<IReadOnlyList<PlayerSearchResultDto>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<PlayerSearchResultDto>>([]);
    }
}
