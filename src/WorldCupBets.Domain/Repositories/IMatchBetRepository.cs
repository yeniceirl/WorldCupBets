using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Domain.Repositories;

public interface IMatchBetRepository
{
    Task<bool> ExistsForUserAndMatchAsync(int userId, int matchId, CancellationToken cancellationToken = default);

    Task<MatchBet?> GetByUserAndMatchAsync(int userId, int matchId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MatchBet>> ListByUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MatchBet>> ListByMatchForSettlementAsync(int matchId, CancellationToken cancellationToken = default);

    Task AddAsync(MatchBet matchBet, CancellationToken cancellationToken = default);
}
