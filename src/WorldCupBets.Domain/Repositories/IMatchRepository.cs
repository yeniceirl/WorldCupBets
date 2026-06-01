using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Domain.Repositories;

public interface IMatchRepository
{
    Task<IReadOnlyList<Match>> ListAsync(CancellationToken cancellationToken = default);

    Task<Match?> GetByIdAsync(int matchId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ListTeamNamesAsync(CancellationToken cancellationToken = default);

    Task<DateTime?> GetChampionBettingClosesAtUtcAsync(CancellationToken cancellationToken = default);
}
