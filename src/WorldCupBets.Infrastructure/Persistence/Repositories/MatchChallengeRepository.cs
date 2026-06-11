using Microsoft.EntityFrameworkCore;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;

namespace WorldCupBets.Infrastructure.Persistence.Repositories;

public sealed class MatchChallengeRepository(AppDbContext dbContext) : IMatchChallengeRepository
{
    public async Task<IReadOnlyList<MatchChallenge>> ListByMatchAsync(int matchId, CancellationToken cancellationToken = default)
    {
        return await dbContext.MatchChallenges
            .AsNoTracking()
            .Include(matchChallenge => matchChallenge.Match)
            .Include(matchChallenge => matchChallenge.Positions)
            .ThenInclude(position => position.User)
            .Where(matchChallenge => matchChallenge.MatchId == matchId)
            .OrderByDescending(matchChallenge => matchChallenge.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public Task<MatchChallenge?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return dbContext.MatchChallenges
            .AsNoTracking()
            .Include(matchChallenge => matchChallenge.Match)
            .Include(matchChallenge => matchChallenge.Positions)
            .ThenInclude(position => position.User)
            .SingleOrDefaultAsync(matchChallenge => matchChallenge.Id == id, cancellationToken);
    }

    public Task<MatchChallenge?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default)
    {
        return dbContext.MatchChallenges
            .Include(matchChallenge => matchChallenge.Match)
            .Include(matchChallenge => matchChallenge.Positions)
            .ThenInclude(position => position.User)
            .SingleOrDefaultAsync(matchChallenge => matchChallenge.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<int, decimal>> ListActiveStakeAmountsByUserAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.MatchChallengePositions
            .AsNoTracking()
            .Where(position => position.MatchChallenge.Status == MatchChallengeStatus.Open || position.MatchChallenge.Status == MatchChallengeStatus.Matched)
            .GroupBy(position => position.UserId)
            .Select(group => new { UserId = group.Key, StakeAmountCc = group.Sum(position => position.StakeAmountCc) })
            .ToDictionaryAsync(item => item.UserId, item => item.StakeAmountCc, cancellationToken);
    }

    public Task AddAsync(MatchChallenge matchChallenge, CancellationToken cancellationToken = default)
    {
        return dbContext.MatchChallenges.AddAsync(matchChallenge, cancellationToken).AsTask();
    }
}
