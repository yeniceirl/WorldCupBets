using Microsoft.EntityFrameworkCore;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;

namespace WorldCupBets.Infrastructure.Persistence.Repositories;

public sealed class TournamentPickRepository(AppDbContext dbContext) : ITournamentPickRepository
{
    public Task<TournamentPick?> GetByUserAndCategoryAsync(int userId, TournamentPickCategory category, CancellationToken cancellationToken = default)
    {
        return dbContext.TournamentPicks
            .AsNoTracking()
            .SingleOrDefaultAsync(tournamentPick => tournamentPick.UserId == userId && tournamentPick.Category == category, cancellationToken);
    }

    public Task<TournamentPick?> GetTrackedByUserAndCategoryAsync(int userId, TournamentPickCategory category, CancellationToken cancellationToken = default)
    {
        return dbContext.TournamentPicks
            .SingleOrDefaultAsync(tournamentPick => tournamentPick.UserId == userId && tournamentPick.Category == category, cancellationToken);
    }

    public async Task<IReadOnlyList<TournamentPick>> ListByUserAndCategoriesAsync(
        int userId,
        IReadOnlyCollection<TournamentPickCategory> categories,
        CancellationToken cancellationToken = default)
    {
        if (categories.Count == 0)
        {
            return [];
        }

        return await dbContext.TournamentPicks
            .AsNoTracking()
            .Where(tournamentPick => tournamentPick.UserId == userId && categories.Contains(tournamentPick.Category))
            .OrderBy(tournamentPick => tournamentPick.Category)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TournamentPick>> ListChampionForSettlementAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.TournamentPicks
            .Include(tournamentPick => tournamentPick.User)
            .Where(tournamentPick => tournamentPick.Category == TournamentPickCategory.Champion)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<int, decimal>> ListStakeAmountsByUserAsync(
        IReadOnlyCollection<TournamentPickCategory> categories,
        CancellationToken cancellationToken = default)
    {
        if (categories.Count == 0)
        {
            return new Dictionary<int, decimal>();
        }

        return await dbContext.TournamentPicks
            .AsNoTracking()
            .Where(tournamentPick => categories.Contains(tournamentPick.Category))
            .GroupBy(tournamentPick => tournamentPick.UserId)
            .Select(group => new { UserId = group.Key, StakeAmountCc = group.Sum(tournamentPick => tournamentPick.StakeAmountCc) })
            .ToDictionaryAsync(item => item.UserId, item => item.StakeAmountCc, cancellationToken);
    }

    public Task AddAsync(TournamentPick pick, CancellationToken cancellationToken = default)
    {
        return dbContext.TournamentPicks.AddAsync(pick, cancellationToken).AsTask();
    }
}
