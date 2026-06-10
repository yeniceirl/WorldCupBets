using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Domain.Repositories;

public interface IMatchChallengeRepository
{
    Task<IReadOnlyList<MatchChallenge>> ListByMatchAsync(int matchId, CancellationToken cancellationToken = default);

    Task<MatchChallenge?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<MatchChallenge?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, decimal>> ListActiveStakeAmountsByUserAsync(CancellationToken cancellationToken = default);

    Task AddAsync(MatchChallenge matchChallenge, CancellationToken cancellationToken = default);
}
