using System.Reflection;
using WorldCupBets.Application.Abstractions;
using WorldCupBets.Application.Features.Leaderboard;
using WorldCupBets.Application.Features.Matches;
using WorldCupBets.Domain.Common;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;
using Xunit;

namespace WorldCupBets.Application.Tests;

public sealed class GetLeaderboardHandlerTests
{
    [Fact]
    public async Task Handle_Returns_Highest_Current_Balance_First()
    {
        var result = await GetLeaderboardHandler.Handle(
            new GetLeaderboardQuery(),
            new StubUserRepository(
                CreateUser(1, "Ada", 1200),
                CreateUser(2, "Grace", 900),
                CreateUser(3, "Linus", 1500)),
            new StubMatchBetRepository(),
            new StubChampionBetRepository(),
            new StubTournamentSettlementRepository(TournamentSettlement.CreateSingleton()),
            CancellationToken.None);

        Assert.Collection(result,
            item =>
            {
                Assert.Equal(1, item.Rank);
                Assert.Equal("Linus", item.DisplayName);
                Assert.Equal(1500, item.CurrentBalanceCc);
                Assert.Equal(0, item.PendingStakeAmountCc);
                Assert.Equal(1500, item.AvailableBalanceCc);
            },
            item =>
            {
                Assert.Equal(2, item.Rank);
                Assert.Equal("Ada", item.DisplayName);
                Assert.Equal(1200, item.CurrentBalanceCc);
                Assert.Equal(0, item.PendingStakeAmountCc);
                Assert.Equal(1200, item.AvailableBalanceCc);
            },
            item =>
            {
                Assert.Equal(3, item.Rank);
                Assert.Equal("Grace", item.DisplayName);
                Assert.Equal(900, item.CurrentBalanceCc);
                Assert.Equal(0, item.PendingStakeAmountCc);
                Assert.Equal(900, item.AvailableBalanceCc);
            });
    }

    [Fact]
    public async Task Handle_Returns_Equal_Balances_Without_Advanced_Tie_Breaker()
    {
        var result = await GetLeaderboardHandler.Handle(
            new GetLeaderboardQuery(),
            new StubUserRepository(
                CreateUser(1, "Ada", 1000),
                CreateUser(2, "Grace", 1000)),
            new StubMatchBetRepository(),
            new StubChampionBetRepository(),
            new StubTournamentSettlementRepository(TournamentSettlement.CreateSingleton()),
            CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, item => Assert.Equal(1000, item.CurrentBalanceCc));
        Assert.Equal(new[] { 1, 2 }, result.Select(item => item.Rank));
    }

    [Fact]
    public async Task Handle_Shows_Pending_Stake_Separately_From_Realized_Balance()
    {
        var ada = CreateUser(1, "Ada", 945);
        var grace = CreateUser(2, "Grace", 990);
        var match = Match.Create(MatchPhase.GroupStage, "Argentina", "Japan", DateTime.UtcNow.AddDays(1), "MetLife Stadium");
        SetEntityId(match, 20);

        var result = await GetLeaderboardHandler.Handle(
            new GetLeaderboardQuery(),
            new StubUserRepository(ada, grace),
            new StubMatchBetRepository(CreateBet(ada, match, MatchBetSelection.Home)),
            new StubChampionBetRepository(ChampionBet.Create(ada.Id, "Argentina", 50, DateTime.UtcNow)),
            new StubTournamentSettlementRepository(TournamentSettlement.CreateSingleton()),
            CancellationToken.None);

        Assert.Collection(result,
            item =>
            {
                Assert.Equal(1, item.Rank);
                Assert.Equal("Ada", item.DisplayName);
                Assert.Equal(1000, item.CurrentBalanceCc);
                Assert.Equal(55, item.PendingStakeAmountCc);
                Assert.Equal(945, item.AvailableBalanceCc);
            },
            item =>
            {
                Assert.Equal(2, item.Rank);
                Assert.Equal("Grace", item.DisplayName);
                Assert.Equal(990, item.CurrentBalanceCc);
                Assert.Equal(0, item.PendingStakeAmountCc);
                Assert.Equal(990, item.AvailableBalanceCc);
            });
    }

    [Fact]
    public async Task Handle_Reflects_Match_Settlement_Balance_Changes()
    {
        var match = Match.Create(MatchPhase.GroupStage, "Argentina", "Japan", DateTime.UtcNow.AddMinutes(-10), "MetLife Stadium");
        SetEntityId(match, 20);
        var winner = CreateUser(1, "Winner", 995);
        var loser = CreateUser(2, "Loser", 995);
        var userRepository = new StubUserRepository(winner, loser);

        var settlement = await RecordMatchResultHandler.Handle(
            new RecordMatchResultCommand(match.Id, MatchBetSelection.Home),
            new StubMatchRepository(match),
            new StubMatchBetRepository(
                CreateBet(winner, match, MatchBetSelection.Home),
                CreateBet(loser, match, MatchBetSelection.Away)),
            new StubTournamentSettlementRepository(TournamentSettlement.CreateSingleton()),
            userRepository,
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        var result = await GetLeaderboardHandler.Handle(
            new GetLeaderboardQuery(),
            userRepository,
            new StubMatchBetRepository(
                CreateBet(winner, match, MatchBetSelection.Home),
                CreateBet(loser, match, MatchBetSelection.Away)),
            new StubChampionBetRepository(),
            new StubTournamentSettlementRepository(TournamentSettlement.CreateSingleton()),
            CancellationToken.None);

        Assert.True(settlement.IsSuccess);
        Assert.Collection(result,
            item =>
            {
                Assert.Equal(1, item.Rank);
                Assert.Equal("Winner", item.DisplayName);
                Assert.Equal(1005, item.CurrentBalanceCc);
                Assert.Equal(0, item.PendingStakeAmountCc);
                Assert.Equal(1005, item.AvailableBalanceCc);
            },
            item =>
            {
                Assert.Equal(2, item.Rank);
                Assert.Equal("Loser", item.DisplayName);
                Assert.Equal(995, item.CurrentBalanceCc);
                Assert.Equal(0, item.PendingStakeAmountCc);
                Assert.Equal(995, item.AvailableBalanceCc);
            });
    }

    private static User CreateUser(int id, string displayName, int balanceCc)
    {
        var user = User.Create($"google-{id}", $"user{id}@example.com", displayName);
        SetEntityId(user, id);
        SetProperty(user, nameof(User.CurrentBalanceCc), balanceCc);
        return user;
    }

    private static MatchBet CreateBet(User user, Match match, MatchBetSelection selection)
    {
        var bet = MatchBet.Create(user.Id, match.Id, selection, match.GetStakeAmountCc(), DateTime.UtcNow.AddMinutes(-20));
        SetProperty(bet, nameof(MatchBet.User), user);
        SetProperty(bet, nameof(MatchBet.Match), match);
        return bet;
    }

    private static void SetEntityId(Entity entity, int id)
    {
        var property = typeof(Entity).GetProperty(nameof(Entity.Id), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        property!.SetValue(entity, id);
    }

    private static void SetProperty(object target, string propertyName, object? value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        property!.SetValue(target, property.PropertyType == typeof(decimal) && value is not null ? Convert.ToDecimal(value) : value);
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

        public Task<IReadOnlyList<string>> ListTeamNamesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<string>>(matches.SelectMany(match => new[] { match.HomeTeamName, match.AwayTeamName }).Distinct().ToArray());
        }

        public Task<DateTime?> GetChampionBettingClosesAtUtcAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<DateTime?>(matches.Where(match => match.Phase != MatchPhase.GroupStage).OrderBy(match => match.StartsAtUtc).Select(match => (DateTime?)match.StartsAtUtc).FirstOrDefault());
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

    private sealed class StubMatchBetRepository(params MatchBet[] matchBets) : IMatchBetRepository
    {
        public Task<bool> ExistsForUserAndMatchAsync(int userId, int matchId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(matchBets.Any(matchBet => matchBet.UserId == userId && matchBet.MatchId == matchId));
        }

        public Task<MatchBet?> GetByUserAndMatchAsync(int userId, int matchId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(matchBets.SingleOrDefault(matchBet => matchBet.UserId == userId && matchBet.MatchId == matchId));
        }

        public Task<IReadOnlyList<MatchBet>> ListByUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<MatchBet>>(matchBets.Where(matchBet => matchBet.UserId == userId).ToArray());
        }

        public Task<IReadOnlyList<MatchBet>> ListByMatchForSettlementAsync(int matchId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<MatchBet>>(matchBets.Where(matchBet => matchBet.MatchId == matchId).ToArray());
        }

        public Task<IReadOnlyDictionary<int, decimal>> ListPendingStakeAmountsByUserAsync(CancellationToken cancellationToken = default)
        {
            var stakesByUser = matchBets
                .Where(matchBet => matchBet.Match.SettledAtUtc is null)
                .GroupBy(matchBet => matchBet.UserId)
                .ToDictionary(group => group.Key, group => group.Sum(matchBet => matchBet.StakeAmountCc));

            return Task.FromResult<IReadOnlyDictionary<int, decimal>>(stakesByUser);
        }

        public Task AddAsync(MatchBet matchBet, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class StubChampionBetRepository(params ChampionBet[] championBets) : IChampionBetRepository
    {
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
            return Task.FromResult<IReadOnlyList<ChampionBet>>(championBets);
        }

        public Task<IReadOnlyDictionary<int, decimal>> ListStakeAmountsByUserAsync(CancellationToken cancellationToken = default)
        {
            var stakesByUser = championBets
                .GroupBy(championBet => championBet.UserId)
                .ToDictionary(group => group.Key, group => group.Sum(championBet => championBet.StakeAmountCc));

            return Task.FromResult<IReadOnlyDictionary<int, decimal>>(stakesByUser);
        }

        public Task AddAsync(ChampionBet championBet, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class StubTournamentSettlementRepository(TournamentSettlement settlement) : ITournamentSettlementRepository
    {
        public Task<TournamentSettlement> GetOrCreateSingletonAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(settlement);
        }

        public Task<bool> IsChampionSettledAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(settlement.ChampionSettledAtUtc.HasValue);
        }
    }
}
