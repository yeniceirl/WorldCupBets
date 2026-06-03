using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Domain.Repositories;

public interface IMatchRepository
{
    Task<IReadOnlyList<Match>> ListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Match>> ListGroupStageFixturesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlySet<int>> ListMatchIdsWithBetsAsync(IEnumerable<int> matchIds, CancellationToken cancellationToken = default);

    Task<Match?> GetByIdAsync(int matchId, CancellationToken cancellationToken = default);

    Task<Match?> GetByIdForSettlementAsync(int matchId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ListTeamNamesAsync(CancellationToken cancellationToken = default);

    Task<DateTime?> GetChampionBettingClosesAtUtcAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Match match, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
