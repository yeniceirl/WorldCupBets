using System.Reflection;
using WorldCupBets.Application.Abstractions;
using WorldCupBets.Application.Features.Bets;
using WorldCupBets.Domain.Common;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;
using Xunit;

namespace WorldCupBets.Application.Tests;

public sealed class PlaceChampionBetHandlerTests
{
    [Fact]
    public async Task Handle_Places_Champion_Bet_And_Deducts_User_Balance()
    {
        var user = User.Create("google-1", "ada@example.com", "Ada");
        SetEntityId(user, 10);

        var result = await PlaceChampionBetHandler.Handle(
            new PlaceChampionBetCommand(user.Id, "Argentina"),
            new StubUserRepository(user),
            new StubMatchRepository(["Argentina", "Japan"], new DateTime(2026, 6, 30, 18, 0, 0, DateTimeKind.Utc)),
            new StubChampionBetRepository(),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(950, user.CurrentBalanceCc);
        Assert.Equal("Argentina", result.Value!.TeamName);
        Assert.Equal(50, result.Value.StakeAmountCc);
        Assert.Equal(950, result.Value.RemainingBalanceCc);
    }

    [Fact]
    public async Task Handle_Rejects_Duplicate_Champion_Bet_For_Same_User()
    {
        var user = User.Create("google-1", "ada@example.com", "Ada");
        SetEntityId(user, 10);
        var existingBet = ChampionBet.Create(user.Id, "Argentina", 50, DateTime.UtcNow);

        var result = await PlaceChampionBetHandler.Handle(
            new PlaceChampionBetCommand(user.Id, "Japan"),
            new StubUserRepository(user),
            new StubMatchRepository(["Argentina", "Japan"], new DateTime(2026, 6, 30, 18, 0, 0, DateTimeKind.Utc)),
            new StubChampionBetRepository(existingBet),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("bets.champion_bet_already_exists", result.Error?.Code);
    }

    [Fact]
    public async Task Handle_Rejects_Champion_Bet_When_Betting_Is_Closed()
    {
        var user = User.Create("google-1", "ada@example.com", "Ada");
        SetEntityId(user, 10);

        var result = await PlaceChampionBetHandler.Handle(
            new PlaceChampionBetCommand(user.Id, "Argentina"),
            new StubUserRepository(user),
            new StubMatchRepository(["Argentina", "Japan"], DateTime.UtcNow.AddMinutes(-1)),
            new StubChampionBetRepository(),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("bets.champion_betting_closed", result.Error?.Code);
    }

    [Fact]
    public async Task Handle_Applies_Dead_Rescue_When_Champion_Bet_Leaves_User_At_Zero()
    {
        var user = User.Create("google-1", "ada@example.com", "Ada");
        SetEntityId(user, 10);
        SetProperty(user, nameof(User.CurrentBalanceCc), 50);

        var result = await PlaceChampionBetHandler.Handle(
            new PlaceChampionBetCommand(user.Id, "Argentina"),
            new StubUserRepository(user),
            new StubMatchRepository(["Argentina", "Japan"], new DateTime(2026, 6, 30, 18, 0, 0, DateTimeKind.Utc)),
            new StubChampionBetRepository(),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(User.DeadRescueAmountCc, user.CurrentBalanceCc);
        Assert.Equal(1, user.RescueCount);
        Assert.Equal(User.DeadRescueAmountCc, user.RescueDebtCc);
        Assert.Equal(User.DeadRescueAmountCc, result.Value!.RemainingBalanceCc);
    }

    private static void SetEntityId(Entity entity, int id)
    {
        var property = typeof(Entity).GetProperty(nameof(Entity.Id), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        property!.SetValue(entity, id);
    }

    private static void SetProperty(object target, string propertyName, object? value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        property!.SetValue(target, value);
    }

    private sealed class StubUserRepository(params User[] users) : IUserRepository
    {
        public Task<User?> GetByGoogleSubjectWithRolesAsync(string googleSubject, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(users.SingleOrDefault(user => user.GoogleSubject == googleSubject));
        }

        public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(users.SingleOrDefault(user => user.Id == userId));
        }

        public Task<IReadOnlyList<User>> ListLeaderboardAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<User>>(users.OrderByDescending(user => user.CurrentBalanceCc).ToArray());
        }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class StubMatchRepository(string[] teamNames, DateTime? championBetClosesAtUtc) : IMatchRepository
    {
        public Task<DateTime?> GetChampionBettingClosesAtUtcAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(championBetClosesAtUtc);
        }

        public Task<Match?> GetByIdAsync(int matchId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<Match?> GetByIdForSettlementAsync(int matchId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<Match>> ListAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<Match>> ListGroupStageFixturesAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlySet<int>> ListMatchIdsWithBetsAsync(IEnumerable<int> matchIds, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<string>> ListTeamNamesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<string>>(teamNames);
        }

        public Task AddAsync(Match match, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class StubChampionBetRepository(params ChampionBet[] seeded) : IChampionBetRepository
    {
        private readonly List<ChampionBet> championBets = [.. seeded];

        public Task<bool> ExistsForUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(championBets.Any(championBet => championBet.UserId == userId));
        }

        public Task<ChampionBet?> GetByUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(championBets.SingleOrDefault(championBet => championBet.UserId == userId));
        }

        public Task<IReadOnlyList<ChampionBet>> ListForSettlementAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ChampionBet>>(championBets.ToArray());
        }

        public Task<IReadOnlyDictionary<int, int>> ListStakeAmountsByUserAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task AddAsync(ChampionBet championBet, CancellationToken cancellationToken = default)
        {
            championBets.Add(championBet);
            return Task.CompletedTask;
        }
    }
}
