using Microsoft.EntityFrameworkCore;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;

namespace WorldCupBets.Infrastructure.Persistence.Repositories;

public sealed class ChampionBetRepository(AppDbContext dbContext) : IChampionBetRepository
{
    public Task<bool> ExistsForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return dbContext.ChampionBets.AnyAsync(championBet => championBet.UserId == userId, cancellationToken);
    }

    public Task<ChampionBet?> GetByUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return dbContext.ChampionBets.AsNoTracking().SingleOrDefaultAsync(championBet => championBet.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<ChampionBet>> ListForSettlementAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.ChampionBets
            .Include(championBet => championBet.User)
            .ToArrayAsync(cancellationToken);
    }

    public Task AddAsync(ChampionBet championBet, CancellationToken cancellationToken = default)
    {
        return dbContext.ChampionBets.AddAsync(championBet, cancellationToken).AsTask();
    }
}
