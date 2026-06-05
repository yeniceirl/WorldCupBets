using Microsoft.EntityFrameworkCore;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;

namespace WorldCupBets.Infrastructure.Persistence.Repositories;

public sealed class SpecialPlayerBetRepository(AppDbContext dbContext) : ISpecialPlayerBetRepository
{
    public Task<bool> ExistsForUserAndCategoryAsync(int userId, SpecialPlayerBetCategory category, CancellationToken cancellationToken = default)
    {
        return dbContext.SpecialPlayerBets.AnyAsync(specialPlayerBet => specialPlayerBet.UserId == userId && specialPlayerBet.Category == category, cancellationToken);
    }

    public async Task<IReadOnlyList<SpecialPlayerBet>> ListByUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.SpecialPlayerBets
            .AsNoTracking()
            .Where(specialPlayerBet => specialPlayerBet.UserId == userId)
            .OrderBy(specialPlayerBet => specialPlayerBet.Category)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<int, decimal>> ListStakeAmountsByUserAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.SpecialPlayerBets
            .AsNoTracking()
            .GroupBy(specialPlayerBet => specialPlayerBet.UserId)
            .Select(group => new { UserId = group.Key, StakeAmountCc = group.Sum(specialPlayerBet => specialPlayerBet.StakeAmountCc) })
            .ToDictionaryAsync(item => item.UserId, item => item.StakeAmountCc, cancellationToken);
    }

    public Task AddAsync(SpecialPlayerBet specialPlayerBet, CancellationToken cancellationToken = default)
    {
        return dbContext.SpecialPlayerBets.AddAsync(specialPlayerBet, cancellationToken).AsTask();
    }
}
