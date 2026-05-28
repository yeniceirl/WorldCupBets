namespace WorldCupBets.Application.Abstractions;

public interface ILookupCacheService
{
    Task<TItem?> GetItemAsync<TItem>(string category, string key, CancellationToken cancellationToken = default)
        where TItem : class;

    Task<IReadOnlyCollection<TItem>> GetCategoryAsync<TItem>(string category, CancellationToken cancellationToken = default)
        where TItem : class;
}
