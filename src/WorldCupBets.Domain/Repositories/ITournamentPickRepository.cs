using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Domain.Repositories;

public interface ITournamentPickRepository
{
    Task<TournamentPick?> GetByUserAndCategoryAsync(int userId, TournamentPickCategory category, CancellationToken cancellationToken = default);

    Task<TournamentPick?> GetTrackedByUserAndCategoryAsync(int userId, TournamentPickCategory category, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TournamentPick>> ListByUserAndCategoriesAsync(int userId, IReadOnlyCollection<TournamentPickCategory> categories, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TournamentPick>> ListChampionForSettlementAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, decimal>> ListStakeAmountsByUserAsync(IReadOnlyCollection<TournamentPickCategory> categories, CancellationToken cancellationToken = default);

    Task AddAsync(TournamentPick pick, CancellationToken cancellationToken = default);
}
