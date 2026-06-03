using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Domain.Repositories;

public interface ITournamentSettlementRepository
{
    Task<TournamentSettlement> GetOrCreateSingletonAsync(CancellationToken cancellationToken = default);
}
