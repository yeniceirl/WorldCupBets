using Microsoft.EntityFrameworkCore;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;

namespace WorldCupBets.Infrastructure.Persistence.Repositories;

public sealed class MatchRepository(AppDbContext dbContext) : IMatchRepository
{
    public async Task<DateTime?> GetChampionBettingClosesAtUtcAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Matches
            .AsNoTracking()
            .Where(match => match.Phase != MatchPhase.GroupStage)
            .OrderBy(match => match.StartsAtUtc)
            .Select(match => (DateTime?)match.StartsAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Match>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Matches
            .AsNoTracking()
            .OrderBy(match => match.StartsAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Match>> ListGroupStageFixturesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Matches
            .Where(match => match.Phase == MatchPhase.GroupStage)
            .OrderBy(match => match.StartsAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ListTeamNamesAsync(CancellationToken cancellationToken = default)
    {
        var homeTeams = dbContext.Matches.AsNoTracking().Select(match => match.HomeTeamName);
        var awayTeams = dbContext.Matches.AsNoTracking().Select(match => match.AwayTeamName);

        return await homeTeams
            .Union(awayTeams)
            .Distinct()
            .OrderBy(teamName => teamName)
            .ToArrayAsync(cancellationToken);
    }

    public Task<Match?> GetByIdAsync(int matchId, CancellationToken cancellationToken = default)
    {
        return dbContext.Matches.SingleOrDefaultAsync(match => match.Id == matchId, cancellationToken);
    }

    public Task<Match?> GetByIdForSettlementAsync(int matchId, CancellationToken cancellationToken = default)
    {
        return dbContext.Matches.SingleOrDefaultAsync(match => match.Id == matchId, cancellationToken);
    }

    public async Task AddAsync(Match match, CancellationToken cancellationToken = default)
    {
        await dbContext.Matches.AddAsync(match, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
