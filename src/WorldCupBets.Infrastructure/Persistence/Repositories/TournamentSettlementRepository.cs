using Microsoft.EntityFrameworkCore;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;

namespace WorldCupBets.Infrastructure.Persistence.Repositories;

public sealed class TournamentSettlementRepository(AppDbContext dbContext) : ITournamentSettlementRepository
{
    public async Task<TournamentSettlement> GetOrCreateSingletonAsync(CancellationToken cancellationToken = default)
    {
        var settlement = await dbContext.TournamentSettlements
            .SingleOrDefaultAsync(item => item.Id == TournamentSettlement.SingletonId, cancellationToken);

        if (settlement is not null)
        {
            return settlement;
        }

        settlement = TournamentSettlement.CreateSingleton();
        await dbContext.TournamentSettlements.AddAsync(settlement, cancellationToken);
        return settlement;
    }
}
