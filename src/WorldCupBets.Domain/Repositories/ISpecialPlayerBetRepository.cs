using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Domain.Repositories;

public interface ISpecialPlayerBetRepository
{
    Task<bool> ExistsForUserAndCategoryAsync(int userId, SpecialPlayerBetCategory category, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SpecialPlayerBet>> ListByUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, decimal>> ListStakeAmountsByUserAsync(CancellationToken cancellationToken = default);

    Task AddAsync(SpecialPlayerBet specialPlayerBet, CancellationToken cancellationToken = default);
}
