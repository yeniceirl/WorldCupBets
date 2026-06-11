using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using WorldCupBets.Application.Abstractions;
using WorldCupBets.Application.Features.Bets;
using WorldCupBets.Application.Features.Challenges;
using WorldCupBets.Application.Features.Matches;
using WorldCupBets.Domain.Common;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Infrastructure.Persistence;
using WorldCupBets.Infrastructure.Persistence.Repositories;
using Xunit;
using Xunit.Abstractions;

namespace WorldCupBets.Application.Tests;

public sealed class PostgresConcurrencyHardeningTests(ITestOutputHelper output)
{
    private const string ConnectionStringEnvironmentVariable = "WORLD_CUP_BETS_TEST_CONNECTION_STRING";

    [Fact]
    public async Task Concurrent_Match_Result_Settlement_Does_Not_Pay_Twice()
    {
        var options = await TryCreateDatabaseAsync(output);
        if (options is null)
        {
            return;
        }

        await using (var dbContext = CreateDbContext(options))
        {
            var match = Match.Create(MatchPhase.GroupStage, "Argentina", "Japan", DateTime.UtcNow.AddMinutes(-10), "MetLife Stadium");
            var winner = CreateUser("winner", 995);
            var loser = CreateUser("loser", 995);

            dbContext.Matches.Add(match);
            dbContext.Users.AddRange(winner, loser);
            dbContext.TournamentSettlements.Add(TournamentSettlement.CreateSingleton());
            await dbContext.SaveChangesAsync();

            dbContext.MatchBets.AddRange(
                MatchBet.Create(winner.Id, match.Id, MatchBetSelection.Home, match.GetStakeAmountCc(), DateTime.UtcNow.AddMinutes(-20)),
                MatchBet.Create(loser.Id, match.Id, MatchBetSelection.Away, match.GetStakeAmountCc(), DateTime.UtcNow.AddMinutes(-20)));
            await dbContext.SaveChangesAsync();
        }

        var matchId = await GetOnlyMatchIdAsync(options);

        var attempts = await Task.WhenAll(
            RecordMatchResultAsync(options, matchId),
            RecordMatchResultAsync(options, matchId));

        await using var verificationContext = CreateDbContext(options);
        var users = await verificationContext.Users.OrderBy(user => user.Email).ToArrayAsync();
        var matchAfterSettlement = await verificationContext.Matches.SingleAsync(match => match.Id == matchId);

        Assert.Contains(attempts, attempt => attempt is { IsConflict: true } || attempt.Result?.Value?.WasAlreadySettled == true);
        Assert.Equal(1005, users.Single(user => user.Email == "winner@example.com").CurrentBalanceCc);
        Assert.Equal(995, users.Single(user => user.Email == "loser@example.com").CurrentBalanceCc);
        Assert.NotNull(matchAfterSettlement.SettledAtUtc);
    }

    [Fact]
    public async Task Concurrent_Champion_Settlement_Does_Not_Pay_Twice()
    {
        var options = await TryCreateDatabaseAsync(output);
        if (options is null)
        {
            return;
        }

        await using (var dbContext = CreateDbContext(options))
        {
            var winner = CreateUser("champion-winner", 950);
            var loser = CreateUser("champion-loser", 950);
            var settlement = TournamentSettlement.CreateSingleton();
            settlement.AddChampionJackpot(20);

            dbContext.Users.AddRange(winner, loser);
            dbContext.TournamentSettlements.Add(settlement);
            await dbContext.SaveChangesAsync();

            dbContext.TournamentPicks.AddRange(
                TournamentPick.CreateChampion(winner.Id, "Argentina", PlaceChampionBetHandler.ChampionBetStakeAmountCc, DateTime.UtcNow.AddDays(-1)),
                TournamentPick.CreateChampion(loser.Id, "Japan", PlaceChampionBetHandler.ChampionBetStakeAmountCc, DateTime.UtcNow.AddDays(-1)));
            await dbContext.SaveChangesAsync();
        }

        var attempts = await Task.WhenAll(
            SettleChampionAsync(options, "Argentina"),
            SettleChampionAsync(options, "Argentina"));

        await using var verificationContext = CreateDbContext(options);
        var users = await verificationContext.Users.OrderBy(user => user.Email).ToArrayAsync();
        var settlementAfterSettlement = await verificationContext.TournamentSettlements.SingleAsync();

        Assert.Contains(attempts, attempt => attempt is { IsConflict: true } || attempt.ChampionResult?.Value?.WasAlreadySettled == true);
        Assert.Equal(1070, users.Single(user => user.Email == "champion-winner@example.com").CurrentBalanceCc);
        Assert.Equal(950, users.Single(user => user.Email == "champion-loser@example.com").CurrentBalanceCc);
        Assert.NotNull(settlementAfterSettlement.ChampionSettledAtUtc);
    }

    [Fact]
    public async Task Concurrent_Match_Bets_For_Same_User_And_Match_Deduct_Only_Once()
    {
        var options = await TryCreateDatabaseAsync(output);
        if (options is null)
        {
            return;
        }

        await using (var dbContext = CreateDbContext(options))
        {
            var user = CreateUser("bettor", User.InitialBalanceCc);
            var match = Match.Create(MatchPhase.GroupStage, "Argentina", "Japan", DateTime.UtcNow.AddHours(1), "MetLife Stadium");

            dbContext.Users.Add(user);
            dbContext.Matches.Add(match);
            await dbContext.SaveChangesAsync();
        }

        var ids = await GetOnlyUserAndMatchIdAsync(options);

        var attempts = await Task.WhenAll(
            PlaceMatchBetAsync(options, ids.UserId, ids.MatchId),
            PlaceMatchBetAsync(options, ids.UserId, ids.MatchId));

        await using var verificationContext = CreateDbContext(options);
        var userAfterBets = await verificationContext.Users.SingleAsync(user => user.Id == ids.UserId);
        var bets = await verificationContext.MatchBets.Where(matchBet => matchBet.UserId == ids.UserId && matchBet.MatchId == ids.MatchId).ToArrayAsync();

        Assert.Contains(attempts, attempt => attempt.IsConflict || attempt.PlaceMatchBetResult?.IsSuccess == true);
        Assert.Single(bets);
        Assert.Equal(995, userAfterBets.CurrentBalanceCc);
    }

    [Fact]
    public async Task MatchChallengeRepository_Lists_By_Match_And_Returns_Active_Stake_Totals()
    {
        var options = await TryCreateDatabaseAsync(output);
        if (options is null)
        {
            return;
        }

        await using (var dbContext = CreateDbContext(options))
        {
            var creator = CreateUser("challenge-creator", User.InitialBalanceCc);
            var taker = CreateUser("challenge-taker", User.InitialBalanceCc);
            var match = Match.Create(MatchPhase.GroupStage, "Argentina", "Japan", DateTime.UtcNow.AddHours(1), "MetLife Stadium");
            var otherMatch = Match.Create(MatchPhase.GroupStage, "Brazil", "Spain", DateTime.UtcNow.AddHours(2), "MetLife Stadium");
            dbContext.Users.AddRange(creator, taker);
            dbContext.Matches.AddRange(match, otherMatch);
            await dbContext.SaveChangesAsync();

            var openChallenge = MatchChallenge.Create(creator.Id, match.Id, "Open claim", "For", "Against", 15, DateTime.UtcNow.AddMinutes(-20));
            var matchedChallenge = MatchChallenge.Create(creator.Id, match.Id, "Matched claim", "For", "Against", 20, DateTime.UtcNow.AddMinutes(-10));
            matchedChallenge.Accept(taker.Id, DateTime.UtcNow.AddMinutes(-5));
            var settledChallenge = MatchChallenge.Create(creator.Id, match.Id, "Settled claim", "For", "Against", 30, DateTime.UtcNow.AddMinutes(-30));
            settledChallenge.Accept(taker.Id, DateTime.UtcNow.AddMinutes(-25));
            settledChallenge.Settle(MatchChallengeSide.Creator, DateTime.UtcNow.AddMinutes(-1));
            var otherMatchChallenge = MatchChallenge.Create(taker.Id, otherMatch.Id, "Other match claim", "For", "Against", 25, DateTime.UtcNow.AddMinutes(-15));

            dbContext.MatchChallenges.AddRange(openChallenge, matchedChallenge, settledChallenge, otherMatchChallenge);
            await dbContext.SaveChangesAsync();
        }

        await using var verificationContext = CreateDbContext(options);
        var repository = new MatchChallengeRepository(verificationContext);
        var matchId = await verificationContext.Matches.Where(match => match.HomeTeamName == "Argentina").Select(match => match.Id).SingleAsync();
        var creatorId = await verificationContext.Users.Where(user => user.Email == "challenge-creator@example.com").Select(user => user.Id).SingleAsync();
        var takerId = await verificationContext.Users.Where(user => user.Email == "challenge-taker@example.com").Select(user => user.Id).SingleAsync();

        var challenges = await repository.ListByMatchAsync(matchId);
        var activeTotals = await repository.ListActiveStakeAmountsByUserAsync();

        Assert.Equal(["Matched claim", "Open claim", "Settled claim"], challenges.Select(challenge => challenge.ClaimText));
        Assert.All(challenges, challenge => Assert.Equal(matchId, challenge.MatchId));
        Assert.Equal(35, activeTotals[creatorId]);
        Assert.Equal(45, activeTotals[takerId]);
    }

    [Fact]
    public async Task Concurrent_Match_Challenge_Accept_Deducts_Only_One_Taker()
    {
        var options = await TryCreateDatabaseAsync(output);
        if (options is null)
        {
            return;
        }

        await using (var dbContext = CreateDbContext(options))
        {
            var creator = CreateUser("challenge-accept-creator", 950);
            var takerOne = CreateUser("challenge-accept-one", User.InitialBalanceCc);
            var takerTwo = CreateUser("challenge-accept-two", User.InitialBalanceCc);
            var match = Match.Create(MatchPhase.GroupStage, "Argentina", "Japan", DateTime.UtcNow.AddHours(1), "MetLife Stadium");
            dbContext.Users.AddRange(creator, takerOne, takerTwo);
            dbContext.Matches.Add(match);
            await dbContext.SaveChangesAsync();

            dbContext.MatchChallenges.Add(MatchChallenge.Create(creator.Id, match.Id, "Race claim", "For", "Against", 50, DateTime.UtcNow.AddMinutes(-1)));
            await dbContext.SaveChangesAsync();
        }

        var ids = await GetChallengeAcceptanceIdsAsync(options);

        var attempts = await Task.WhenAll(
            AcceptMatchChallengeAsync(options, ids.ChallengeId, ids.TakerOneId),
            AcceptMatchChallengeAsync(options, ids.ChallengeId, ids.TakerTwoId));

        await using var verificationContext = CreateDbContext(options);
        var challenge = await verificationContext.MatchChallenges.Include(item => item.Positions).SingleAsync(item => item.Id == ids.ChallengeId);
        var takers = await verificationContext.Users.Where(user => user.Id == ids.TakerOneId || user.Id == ids.TakerTwoId).ToArrayAsync();

        Assert.Contains(attempts, attempt => attempt.IsConflict || attempt.MutationResult?.IsFailure == true);
        Assert.Equal(MatchChallengeStatus.Matched, challenge.Status);
        Assert.Equal(2, challenge.Positions.Count);
        Assert.Single(takers, user => user.CurrentBalanceCc == 950);
        Assert.Single(takers, user => user.CurrentBalanceCc == User.InitialBalanceCc);
    }

    [Fact]
    public async Task Concurrent_Match_Challenge_Settlement_Pays_Only_Once()
    {
        var options = await TryCreateDatabaseAsync(output);
        if (options is null)
        {
            return;
        }

        await using (var dbContext = CreateDbContext(options))
        {
            var creator = CreateUser("challenge-settle-creator", 950);
            var taker = CreateUser("challenge-settle-taker", 950);
            var match = Match.Create(MatchPhase.GroupStage, "Argentina", "Japan", DateTime.UtcNow.AddHours(1), "MetLife Stadium");
            dbContext.Users.AddRange(creator, taker);
            dbContext.Matches.Add(match);
            await dbContext.SaveChangesAsync();

            var challengeToSettle = MatchChallenge.Create(creator.Id, match.Id, "Settle race claim", "For", "Against", 50, DateTime.UtcNow.AddMinutes(-2));
            challengeToSettle.Accept(taker.Id, DateTime.UtcNow.AddMinutes(-1));
            dbContext.MatchChallenges.Add(challengeToSettle);
            await dbContext.SaveChangesAsync();
        }

        var challengeId = await GetOnlyChallengeIdAsync(options);

        var attempts = await Task.WhenAll(
            SettleMatchChallengeAsync(options, challengeId, MatchChallengeSide.Creator),
            SettleMatchChallengeAsync(options, challengeId, MatchChallengeSide.Creator));

        await using var verificationContext = CreateDbContext(options);
        var challenge = await verificationContext.MatchChallenges.SingleAsync(item => item.Id == challengeId);
        var users = await verificationContext.Users.OrderBy(user => user.Email).ToArrayAsync();

        Assert.Contains(attempts, attempt => attempt.IsConflict || attempt.LifecycleResult?.IsFailure == true);
        Assert.Equal(MatchChallengeStatus.Settled, challenge.Status);
        Assert.Equal(1050, users.Single(user => user.Email == "challenge-settle-creator@example.com").CurrentBalanceCc);
        Assert.Equal(950, users.Single(user => user.Email == "challenge-settle-taker@example.com").CurrentBalanceCc);
    }

    private static async Task<ConcurrencyAttempt> RecordMatchResultAsync(DbContextOptions<AppDbContext> options, int matchId)
    {
        try
        {
            await using var dbContext = CreateDbContext(options);
            var result = await RecordMatchResultHandler.Handle(
                new RecordMatchResultCommand(matchId, MatchBetSelection.Home),
                new MatchRepository(dbContext),
                new MatchBetRepository(dbContext),
                new TournamentSettlementRepository(dbContext),
                new UserRepository(dbContext),
                new EfApplicationTransactionFactory(dbContext),
                CancellationToken.None);

            return new ConcurrencyAttempt(result, null, null, false);
        }
        catch (PersistenceConflictException)
        {
            return new ConcurrencyAttempt(null, null, null, true);
        }
    }

    private static async Task<ConcurrencyAttempt> SettleChampionAsync(DbContextOptions<AppDbContext> options, string championTeamName)
    {
        try
        {
            await using var dbContext = CreateDbContext(options);
            var result = await SettleChampionHandler.Handle(
                new SettleChampionCommand(championTeamName),
                new TournamentPickRepository(dbContext),
                new TournamentSettlementRepository(dbContext),
                new UserRepository(dbContext),
                new EfApplicationTransactionFactory(dbContext),
                CancellationToken.None);

            return new ConcurrencyAttempt(null, result, null, false);
        }
        catch (PersistenceConflictException)
        {
            return new ConcurrencyAttempt(null, null, null, true);
        }
    }

    private static async Task<ConcurrencyAttempt> PlaceMatchBetAsync(DbContextOptions<AppDbContext> options, int userId, int matchId)
    {
        try
        {
            await using var dbContext = CreateDbContext(options);
            var result = await PlaceMatchBetHandler.Handle(
                new PlaceMatchBetCommand(userId, matchId, MatchBetSelection.Home),
                new UserRepository(dbContext),
                new MatchRepository(dbContext),
                new MatchBetRepository(dbContext),
                new EfApplicationTransactionFactory(dbContext),
                CancellationToken.None);

            return new ConcurrencyAttempt(null, null, result, false);
        }
        catch (PersistenceConflictException)
        {
            return new ConcurrencyAttempt(null, null, null, true);
        }
    }

    private static async Task<ChallengeConcurrencyAttempt> AcceptMatchChallengeAsync(DbContextOptions<AppDbContext> options, int challengeId, int takerUserId)
    {
        try
        {
            await using var dbContext = CreateDbContext(options);
            var result = await AcceptChallengeHandler.Handle(
                new AcceptChallengeCommand(challengeId, takerUserId),
                new UserRepository(dbContext),
                new MatchRepository(dbContext),
                new MatchChallengeRepository(dbContext),
                new EfApplicationTransactionFactory(dbContext),
                CancellationToken.None);

            return new ChallengeConcurrencyAttempt(result, null, false);
        }
        catch (PersistenceConflictException)
        {
            return new ChallengeConcurrencyAttempt(null, null, true);
        }
    }

    private static async Task<ChallengeConcurrencyAttempt> SettleMatchChallengeAsync(DbContextOptions<AppDbContext> options, int challengeId, MatchChallengeSide winnerSide)
    {
        try
        {
            await using var dbContext = CreateDbContext(options);
            var result = await SettleChallengeHandler.Handle(
                new SettleChallengeCommand(challengeId, winnerSide),
                new UserRepository(dbContext),
                new MatchChallengeRepository(dbContext),
                new EfApplicationTransactionFactory(dbContext),
                CancellationToken.None);

            return new ChallengeConcurrencyAttempt(null, result, false);
        }
        catch (PersistenceConflictException)
        {
            return new ChallengeConcurrencyAttempt(null, null, true);
        }
    }

    private static async Task<int> GetOnlyMatchIdAsync(DbContextOptions<AppDbContext> options)
    {
        await using var dbContext = CreateDbContext(options);
        return await dbContext.Matches.Select(match => match.Id).SingleAsync();
    }

    private static async Task<(int UserId, int MatchId)> GetOnlyUserAndMatchIdAsync(DbContextOptions<AppDbContext> options)
    {
        await using var dbContext = CreateDbContext(options);
        var userId = await dbContext.Users.Select(user => user.Id).SingleAsync();
        var matchId = await dbContext.Matches.Select(match => match.Id).SingleAsync();
        return (userId, matchId);
    }

    private static async Task<int> GetOnlyChallengeIdAsync(DbContextOptions<AppDbContext> options)
    {
        await using var dbContext = CreateDbContext(options);
        return await dbContext.MatchChallenges.Select(challenge => challenge.Id).SingleAsync();
    }

    private static async Task<(int ChallengeId, int TakerOneId, int TakerTwoId)> GetChallengeAcceptanceIdsAsync(DbContextOptions<AppDbContext> options)
    {
        await using var dbContext = CreateDbContext(options);
        var challengeId = await dbContext.MatchChallenges.Select(challenge => challenge.Id).SingleAsync();
        var takerOneId = await dbContext.Users.Where(user => user.Email == "challenge-accept-one@example.com").Select(user => user.Id).SingleAsync();
        var takerTwoId = await dbContext.Users.Where(user => user.Email == "challenge-accept-two@example.com").Select(user => user.Id).SingleAsync();
        return (challengeId, takerOneId, takerTwoId);
    }

    private static async Task<DbContextOptions<AppDbContext>?> TryCreateDatabaseAsync(ITestOutputHelper output)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            output.WriteLine($"Skipping PostgreSQL concurrency test because {ConnectionStringEnvironmentVariable} is not set.");
            return null;
        }

        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);
        if (connectionStringBuilder.Database is null || !connectionStringBuilder.Database.Contains("test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"{ConnectionStringEnvironmentVariable} must point to a disposable database whose name contains 'test'.");
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var dbContext = CreateDbContext(options);
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
        return options;
    }

    private static AppDbContext CreateDbContext(DbContextOptions<AppDbContext> options)
    {
        return new AppDbContext(options);
    }

    private static User CreateUser(string key, decimal balanceCc)
    {
        var user = User.Create($"google-{key}", $"{key}@example.com", key);
        SetProperty(user, nameof(User.CurrentBalanceCc), balanceCc);
        return user;
    }

    private static void SetProperty(object target, string propertyName, object? value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        property!.SetValue(target, property.PropertyType == typeof(decimal) && value is not null ? Convert.ToDecimal(value) : value);
    }

    private sealed record ConcurrencyAttempt(
        Result<RecordMatchResultDto>? Result,
        Result<SettleChampionResultDto>? ChampionResult,
        Result<PlaceMatchBetResultDto>? PlaceMatchBetResult,
        bool IsConflict);

    private sealed record ChallengeConcurrencyAttempt(
        Result<ChallengeMutationResultDto>? MutationResult,
        Result<ChallengeDto>? LifecycleResult,
        bool IsConflict);
}
