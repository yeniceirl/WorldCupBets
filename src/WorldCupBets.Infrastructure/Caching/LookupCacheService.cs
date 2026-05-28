using WorldCupBets.Application.Abstractions;

namespace WorldCupBets.Infrastructure.Caching;

public sealed class LookupCacheService : ILookupCacheService
{
    public Task<IReadOnlyCollection<TItem>> GetCategoryAsync<TItem>(string category, CancellationToken cancellationToken = default)
        where TItem : class
    {
        IReadOnlyCollection<TItem> items = Array.Empty<TItem>();
        return Task.FromResult(items);
    }

    public Task<TItem?> GetItemAsync<TItem>(string category, string key, CancellationToken cancellationToken = default)
        where TItem : class
    {
        return Task.FromResult<TItem?>(null);
    }
}
