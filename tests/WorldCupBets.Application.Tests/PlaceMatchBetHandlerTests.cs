using System.Reflection;
using WorldCupBets.Application.Abstractions;
using WorldCupBets.Application.Features.Bets;
using WorldCupBets.Domain.Common;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;
using Xunit;

namespace WorldCupBets.Application.Tests;

public sealed class PlaceMatchBetHandlerTests
{
    [Fact]
    public async Task Handle_Places_Bet_And_Deducts_User_Balance()
    {
        var user = User.Create("google-1", "ada@example.com", "Ada");
        SetEntityId(user, 10);
        var match = Match.Create(MatchPhase.GroupStage, "Argentina", "Japan", DateTime.UtcNow.AddHours(1), "MetLife Stadium");
        SetEntityId(match, 20);

        var userRepository = new StubUserRepository(user);
        var matchRepository = new StubMatchRepository(match);
        var matchBetRepository = new StubMatchBetRepository();

        var result = await PlaceMatchBetHandler.Handle(
            new PlaceMatchBetCommand(user.Id, match.Id, MatchBetSelection.Home),
            userRepository,
            matchRepository,
            matchBetRepository,
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(1, matchBetRepository.AddCalls);
        Assert.Equal(995, user.CurrentBalanceCc);
        Assert.Equal("Home", result.Value!.Selection);
        Assert.Equal(5, result.Value.StakeAmountCc);
        Assert.Equal(995, result.Value.RemainingBalanceCc);
        Assert.Equal(match.Id, matchBetRepository.Stored.Single().MatchId);
    }

    [Fact]
    public async Task Handle_Changes_Existing_Bet_Selection_While_Betting_Is_Open()
    {
        var user = User.Create("google-1", "ada@example.com", "Ada");
        SetEntityId(user, 10);
        var match = Match.Create(MatchPhase.GroupStage, "Argentina", "Japan", DateTime.UtcNow.AddHours(1), "MetLife Stadium");
        SetEntityId(match, 20);
        var existingBet = MatchBet.Create(user.Id, match.Id, MatchBetSelection.Draw, 5, DateTime.UtcNow);
        var matchBetRepository = new StubMatchBetRepository(existingBet);

        var result = await PlaceMatchBetHandler.Handle(
            new PlaceMatchBetCommand(user.Id, match.Id, MatchBetSelection.Home),
            new StubUserRepository(user),
            new StubMatchRepository(match),
            matchBetRepository,
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Home", result.Value?.Selection);
        Assert.Equal(MatchBetSelection.Home, existingBet.Selection);
        Assert.Equal(User.InitialBalanceCc, user.CurrentBalanceCc);
        Assert.Equal(0, matchBetRepository.AddCalls);
    }

    [Fact]
    public async Task Handle_Rejects_Bet_When_Betting_Is_Closed()
    {
        var user = User.Create("google-1", "ada@example.com", "Ada");
        SetEntityId(user, 10);
        var match = Match.Create(MatchPhase.GroupStage, "Argentina", "Japan", DateTime.UtcNow.AddMinutes(-6), "MetLife Stadium");
        SetEntityId(match, 20);

        var result = await PlaceMatchBetHandler.Handle(
            new PlaceMatchBetCommand(user.Id, match.Id, MatchBetSelection.Home),
            new StubUserRepository(user),
            new StubMatchRepository(match),
            new StubMatchBetRepository(),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("bets.match_betting_closed", result.Error?.Code);
        Assert.Equal(User.InitialBalanceCc, user.CurrentBalanceCc);
    }

    [Fact]
    public async Task Handle_Rejects_Changing_Existing_Bet_When_Betting_Is_Closed()
    {
        var user = User.Create("google-1", "ada@example.com", "Ada");
        SetEntityId(user, 10);
        var match = Match.Create(MatchPhase.GroupStage, "Argentina", "Japan", DateTime.UtcNow.AddMinutes(-6), "MetLife Stadium");
        SetEntityId(match, 20);
        var existingBet = MatchBet.Create(user.Id, match.Id, MatchBetSelection.Draw, 5, DateTime.UtcNow.AddMinutes(-10));

        var result = await PlaceMatchBetHandler.Handle(
            new PlaceMatchBetCommand(user.Id, match.Id, MatchBetSelection.Home),
            new StubUserRepository(user),
            new StubMatchRepository(match),
            new StubMatchBetRepository(existingBet),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("bets.match_betting_closed", result.Error?.Code);
        Assert.Equal(MatchBetSelection.Draw, existingBet.Selection);
        Assert.Equal(User.InitialBalanceCc, user.CurrentBalanceCc);
    }

    [Fact]
    public async Task Handle_Rejects_Bet_When_User_Cannot_Afford_Stake()
    {
        var user = User.Create("google-1", "ada@example.com", "Ada");
        SetEntityId(user, 10);
        SetProperty(user, nameof(User.CurrentBalanceCc), 4);
        var match = Match.Create(MatchPhase.GroupStage, "Argentina", "Japan", DateTime.UtcNow.AddHours(1), "MetLife Stadium");
        SetEntityId(match, 20);

        var matchBetRepository = new StubMatchBetRepository();

        var result = await PlaceMatchBetHandler.Handle(
            new PlaceMatchBetCommand(user.Id, match.Id, MatchBetSelection.Home),
            new StubUserRepository(user),
            new StubMatchRepository(match),
            matchBetRepository,
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("bets.insufficient_balance", result.Error?.Code);
        Assert.Empty(matchBetRepository.Stored);
    }

    [Fact]
    public async Task Handle_Applies_Dead_Rescue_When_Match_Bet_Leaves_User_At_Zero()
    {
        var user = User.Create("google-1", "ada@example.com", "Ada");
        SetEntityId(user, 10);
        SetProperty(user, nameof(User.CurrentBalanceCc), 5);
        var match = Match.Create(MatchPhase.GroupStage, "Argentina", "Japan", DateTime.UtcNow.AddHours(1), "MetLife Stadium");
        SetEntityId(match, 20);

        var result = await PlaceMatchBetHandler.Handle(
            new PlaceMatchBetCommand(user.Id, match.Id, MatchBetSelection.Home),
            new StubUserRepository(user),
            new StubMatchRepository(match),
            new StubMatchBetRepository(),
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

    private sealed class StubMatchRepository(params Match[] matches) : IMatchRepository
    {
        public Task<IReadOnlyList<string>> ListTeamNamesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<string>>(matches
                .SelectMany(match => new[] { match.HomeTeamName, match.AwayTeamName })
                .Distinct()
                .ToArray());
        }

        public Task<DateTime?> GetChampionBettingClosesAtUtcAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<DateTime?>(matches
                .Where(match => match.Phase != MatchPhase.GroupStage)
                .OrderBy(match => match.StartsAtUtc)
                .Select(match => (DateTime?)match.StartsAtUtc)
                .FirstOrDefault());
        }

        public Task<IReadOnlyList<Match>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Match>>(matches);
        }

        public Task<IReadOnlyList<Match>> ListGroupStageFixturesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Match>>(matches.Where(match => match.Phase == MatchPhase.GroupStage).ToArray());
        }

        public Task<IReadOnlySet<int>> ListMatchIdsWithBetsAsync(IEnumerable<int> matchIds, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlySet<int>>(new HashSet<int>());
        }

        public Task<Match?> GetByIdAsync(int matchId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(matches.SingleOrDefault(match => match.Id == matchId));
        }

        public Task<Match?> GetByIdForSettlementAsync(int matchId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(matches.SingleOrDefault(match => match.Id == matchId));
        }

        public Task AddAsync(Match match, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class StubMatchBetRepository(params MatchBet[] seeded) : IMatchBetRepository
    {
        public List<MatchBet> Stored { get; } = [.. seeded];

        public int AddCalls { get; private set; }

        public Task<bool> ExistsForUserAndMatchAsync(int userId, int matchId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Stored.Any(matchBet => matchBet.UserId == userId && matchBet.MatchId == matchId));
        }

        public Task<MatchBet?> GetByUserAndMatchAsync(int userId, int matchId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Stored.SingleOrDefault(matchBet => matchBet.UserId == userId && matchBet.MatchId == matchId));
        }

        public Task<IReadOnlyList<MatchBet>> ListByUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<MatchBet>>(Stored.Where(matchBet => matchBet.UserId == userId).ToArray());
        }

        public Task<IReadOnlyList<MatchBet>> ListByMatchForSettlementAsync(int matchId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<MatchBet>>(Stored.Where(matchBet => matchBet.MatchId == matchId).ToArray());
        }

        public Task<IReadOnlyDictionary<int, int>> ListPendingStakeAmountsByUserAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task AddAsync(MatchBet matchBet, CancellationToken cancellationToken = default)
        {
            AddCalls++;
            Stored.Add(matchBet);
            return Task.CompletedTask;
        }
    }
}
