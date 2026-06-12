using Microsoft.EntityFrameworkCore;
using WorldCupBets.Application.Abstractions;
using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Infrastructure.Persistence.Repositories;

public sealed class AuditReadRepository(AppDbContext dbContext) : IAuditReadRepository
{
    public async Task<IReadOnlyList<AuditUserReadModel>> ListUsersAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.DisplayName)
            .Select(user => new AuditUserReadModel(
                user.Id,
                user.DisplayName,
                user.Email,
                user.CurrentBalanceCc,
                user.RescueCount,
                user.RescueDebtCc))
            .ToArrayAsync(cancellationToken);
    }

    public Task<AuditUserReadModel?> GetUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new AuditUserReadModel(
                user.Id,
                user.DisplayName,
                user.Email,
                user.CurrentBalanceCc,
                user.RescueCount,
                user.RescueDebtCc))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditMatchBetReadModel>> ListMatchBetsAsync(int? userId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.MatchBets.AsNoTracking();
        if (userId.HasValue)
        {
            query = query.Where(matchBet => matchBet.UserId == userId.Value);
        }

        return await query
            .Select(matchBet => new AuditMatchBetReadModel(
                matchBet.Id,
                matchBet.UserId,
                matchBet.Selection,
                matchBet.StakeAmountCc,
                matchBet.PlacedAtUtc,
                matchBet.MatchId,
                matchBet.Match.HomeTeamName,
                matchBet.Match.AwayTeamName,
                matchBet.Match.StartsAtUtc,
                matchBet.Match.OfficialResult,
                matchBet.Match.SettledAtUtc))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditChallengePositionReadModel>> ListChallengePositionsAsync(int? userId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.MatchChallengePositions.AsNoTracking();
        if (userId.HasValue)
        {
            query = query.Where(position => position.UserId == userId.Value);
        }

        return await query
            .Select(position => new AuditChallengePositionReadModel(
                position.MatchChallengeId,
                position.UserId,
                position.Side,
                position.StakeAmountCc,
                position.EscrowedAtUtc,
                position.MatchChallenge.Status,
                position.MatchChallenge.WinnerSide,
                position.MatchChallenge.ClaimText,
                position.MatchChallenge.CreatorSideText,
                position.MatchChallenge.TakerSideText,
                position.MatchChallenge.Match.HomeTeamName,
                position.MatchChallenge.Match.AwayTeamName,
                position.MatchChallenge.Positions.Count,
                position.MatchChallenge.Positions.Sum(item => item.StakeAmountCc)))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditTournamentPickReadModel>> ListTournamentPicksAsync(int? userId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.TournamentPicks.AsNoTracking();
        if (userId.HasValue)
        {
            query = query.Where(tournamentPick => tournamentPick.UserId == userId.Value);
        }

        return await query
            .Select(tournamentPick => new AuditTournamentPickReadModel(
                tournamentPick.Id,
                tournamentPick.UserId,
                tournamentPick.Category,
                tournamentPick.SelectedText,
                tournamentPick.StakeAmountCc,
                tournamentPick.PlacedAtUtc))
            .ToArrayAsync(cancellationToken);
    }

    public Task<AuditTournamentSettlementReadModel?> GetTournamentSettlementAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.TournamentSettlements
            .AsNoTracking()
            .Where(settlement => settlement.Id == TournamentSettlement.SingletonId)
            .Select(settlement => new AuditTournamentSettlementReadModel(
                settlement.ChampionTeamName,
                settlement.ChampionSettledAtUtc,
                settlement.ChampionJackpotCc,
                settlement.UndistributedJackpotCc))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
