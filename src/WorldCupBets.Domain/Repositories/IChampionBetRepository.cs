using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Domain.Repositories;

public interface IChampionBetRepository
{
    Task<bool> ExistsForUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<ChampionBet?> GetByUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChampionBet>> ListForSettlementAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, decimal>> ListStakeAmountsByUserAsync(CancellationToken cancellationToken = default);

    Task AddAsync(ChampionBet championBet, CancellationToken cancellationToken = default);
}
