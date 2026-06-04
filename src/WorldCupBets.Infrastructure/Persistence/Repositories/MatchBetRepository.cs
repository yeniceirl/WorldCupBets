using Microsoft.EntityFrameworkCore;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;

namespace WorldCupBets.Infrastructure.Persistence.Repositories;

public sealed class MatchBetRepository(AppDbContext dbContext) : IMatchBetRepository
{
    public Task<bool> ExistsForUserAndMatchAsync(int userId, int matchId, CancellationToken cancellationToken = default)
    {
        return dbContext.Set<MatchBet>().AnyAsync(matchBet => matchBet.UserId == userId && matchBet.MatchId == matchId, cancellationToken);
    }

    public Task<MatchBet?> GetByUserAndMatchAsync(int userId, int matchId, CancellationToken cancellationToken = default)
    {
        return dbContext.Set<MatchBet>().SingleOrDefaultAsync(matchBet => matchBet.UserId == userId && matchBet.MatchId == matchId, cancellationToken);
    }

    public async Task<IReadOnlyList<MatchBet>> ListByUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<MatchBet>()
            .AsNoTracking()
            .Where(matchBet => matchBet.UserId == userId)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MatchBet>> ListByMatchForSettlementAsync(int matchId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<MatchBet>()
            .Include(matchBet => matchBet.User)
            .Where(matchBet => matchBet.MatchId == matchId)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<int, int>> ListPendingStakeAmountsByUserAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<MatchBet>()
            .AsNoTracking()
            .Where(matchBet => matchBet.Match.SettledAtUtc == null)
            .GroupBy(matchBet => matchBet.UserId)
            .Select(group => new { UserId = group.Key, StakeAmountCc = group.Sum(matchBet => matchBet.StakeAmountCc) })
            .ToDictionaryAsync(item => item.UserId, item => item.StakeAmountCc, cancellationToken);
    }

    public Task AddAsync(MatchBet matchBet, CancellationToken cancellationToken = default)
    {
        return dbContext.Set<MatchBet>().AddAsync(matchBet, cancellationToken).AsTask();
    }
}
