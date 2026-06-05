using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using WorldCupBets.Application.Abstractions;
using WorldCupBets.Application.Features.Bets;
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

            dbContext.ChampionBets.AddRange(
                ChampionBet.Create(winner.Id, "Argentina", PlaceChampionBetHandler.ChampionBetStakeAmountCc, DateTime.UtcNow.AddDays(-1)),
                ChampionBet.Create(loser.Id, "Japan", PlaceChampionBetHandler.ChampionBetStakeAmountCc, DateTime.UtcNow.AddDays(-1)));
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
                new ChampionBetRepository(dbContext),
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
}
