using System.Reflection;
using WorldCupBets.Application.Abstractions;
using WorldCupBets.Application.Features.Bets;
using WorldCupBets.Domain.Common;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;
using Xunit;

namespace WorldCupBets.Application.Tests;

public sealed class PlaceSpecialPlayerBetHandlerTests
{
    [Fact]
    public async Task Handle_Places_Special_Player_Bet_And_Deducts_User_Balance()
    {
        var user = User.Create("google-1", "ada@example.com", "Ada");
        SetEntityId(user, 10);

        var result = await PlaceSpecialPlayerBetHandler.Handle(
            new PlaceSpecialPlayerBetCommand(user.Id, SpecialPlayerBetCategory.BestPlayer, "Lionel Messi", "34146370"),
            new StubUserRepository(user),
            new StubMatchRepository(new DateTime(2026, 6, 28, 18, 0, 0, DateTimeKind.Utc)),
            new StubSpecialPlayerBetRepository(),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(950, user.CurrentBalanceCc);
        Assert.Equal("BestPlayer", result.Value!.Category);
        Assert.Equal("Lionel Messi", result.Value.PlayerName);
        Assert.Equal("34146370", result.Value.ExternalPlayerId);
        Assert.Equal(50, result.Value.StakeAmountCc);
        Assert.Equal(950, result.Value.RemainingBalanceCc);
    }

    [Fact]
    public async Task Handle_Rejects_Duplicate_Special_Player_Bet_For_Same_Category()
    {
        var user = User.Create("google-1", "ada@example.com", "Ada");
        SetEntityId(user, 10);
        var existingBet = SpecialPlayerBet.Create(user.Id, SpecialPlayerBetCategory.TopScorer, "Kylian Mbappe", "34161330", 50, DateTime.UtcNow);

        var result = await PlaceSpecialPlayerBetHandler.Handle(
            new PlaceSpecialPlayerBetCommand(user.Id, SpecialPlayerBetCategory.TopScorer, "Erling Haaland", "34161052"),
            new StubUserRepository(user),
            new StubMatchRepository(new DateTime(2026, 6, 28, 18, 0, 0, DateTimeKind.Utc)),
            new StubSpecialPlayerBetRepository(existingBet),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("bets.special_player_bet_already_exists", result.Error?.Code);
        Assert.Equal(1000, user.CurrentBalanceCc);
    }

    [Fact]
    public async Task Handle_Rejects_Special_Player_Bet_When_Betting_Is_Closed()
    {
        var user = User.Create("google-1", "ada@example.com", "Ada");
        SetEntityId(user, 10);

        var result = await PlaceSpecialPlayerBetHandler.Handle(
            new PlaceSpecialPlayerBetCommand(user.Id, SpecialPlayerBetCategory.BestPlayer, "Lionel Messi", "34146370"),
            new StubUserRepository(user),
            new StubMatchRepository(DateTime.UtcNow.AddMinutes(-1)),
            new StubSpecialPlayerBetRepository(),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("bets.special_betting_closed", result.Error?.Code);
        Assert.Equal(1000, user.CurrentBalanceCc);
    }

    [Fact]
    public async Task Handle_Rejects_Player_Name_Shorter_Than_Three_Characters()
    {
        var user = User.Create("google-1", "ada@example.com", "Ada");
        SetEntityId(user, 10);

        var result = await PlaceSpecialPlayerBetHandler.Handle(
            new PlaceSpecialPlayerBetCommand(user.Id, SpecialPlayerBetCategory.BestPlayer, "Li", null),
            new StubUserRepository(user),
            new StubMatchRepository(new DateTime(2026, 6, 28, 18, 0, 0, DateTimeKind.Utc)),
            new StubSpecialPlayerBetRepository(),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("bets.invalid_player_name", result.Error?.Code);
        Assert.Equal(1000, user.CurrentBalanceCc);
    }

    private static void SetEntityId(Entity entity, int id)
    {
        var property = typeof(Entity).GetProperty(nameof(Entity.Id), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        property!.SetValue(entity, id);
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

    private sealed class StubMatchRepository(DateTime? championBetClosesAtUtc) : IMatchRepository
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
            throw new NotSupportedException();
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

    private sealed class StubSpecialPlayerBetRepository(params SpecialPlayerBet[] seeded) : ISpecialPlayerBetRepository
    {
        private readonly List<SpecialPlayerBet> specialPlayerBets = [.. seeded];

        public Task<bool> ExistsForUserAndCategoryAsync(int userId, SpecialPlayerBetCategory category, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(specialPlayerBets.Any(specialPlayerBet => specialPlayerBet.UserId == userId && specialPlayerBet.Category == category));
        }

        public Task<IReadOnlyList<SpecialPlayerBet>> ListByUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<SpecialPlayerBet>>(specialPlayerBets.Where(specialPlayerBet => specialPlayerBet.UserId == userId).ToArray());
        }

        public Task<IReadOnlyDictionary<int, decimal>> ListStakeAmountsByUserAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task AddAsync(SpecialPlayerBet specialPlayerBet, CancellationToken cancellationToken = default)
        {
            specialPlayerBets.Add(specialPlayerBet);
            return Task.CompletedTask;
        }
    }
}
