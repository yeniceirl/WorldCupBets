using System.Reflection;
using WorldCupBets.Application.Abstractions;
using WorldCupBets.Application.Features.Matches;
using WorldCupBets.Domain.Common;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;
using Xunit;

namespace WorldCupBets.Application.Tests;

public sealed class RecordMatchResultHandlerTests
{
    [Fact]
    public async Task Handle_Records_Result_After_Betting_Window_Closes()
    {
        var match = CreateClosedMatch();
        var user = CreateUser(1, 995);
        var bet = CreateBet(user, match, MatchBetSelection.Home);
        var settlement = TournamentSettlement.CreateSingleton();
        var userRepository = new StubUserRepository(user);

        var result = await RecordMatchResultHandler.Handle(
            new RecordMatchResultCommand(match.Id, MatchBetSelection.Home),
            new StubMatchRepository(match),
            new StubMatchBetRepository(bet),
            new StubTournamentSettlementRepository(settlement),
            userRepository,
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(MatchBetSelection.Home, match.OfficialResult);
        Assert.NotNull(match.SettledAtUtc);
        Assert.False(result.Value!.WasAlreadySettled);
        Assert.Equal(1000, user.CurrentBalanceCc);
        Assert.Equal(1, userRepository.SaveCalls);
    }

    [Fact]
    public async Task Handle_Rejects_Result_Before_Betting_Window_Closes()
    {
        var match = CreateOpenMatch();
        var user = CreateUser(1, 995);
        var bet = CreateBet(user, match, MatchBetSelection.Home);

        var result = await RecordMatchResultHandler.Handle(
            new RecordMatchResultCommand(match.Id, MatchBetSelection.Home),
            new StubMatchRepository(match),
            new StubMatchBetRepository(bet),
            new StubTournamentSettlementRepository(TournamentSettlement.CreateSingleton()),
            new StubUserRepository(user),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("matches.result_window_open", result.Error?.Code);
        Assert.Null(match.OfficialResult);
        Assert.Null(match.SettledAtUtc);
        Assert.Equal(995, user.CurrentBalanceCc);
    }

    [Fact]
    public async Task Handle_Credits_Remainder_To_Champion_Jackpot()
    {
        var match = CreateClosedMatch();
        var winnerOne = CreateUser(1, 995);
        var winnerTwo = CreateUser(2, 995);
        var winnerThree = CreateUser(3, 995);
        var loserOne = CreateUser(4, 995);
        var loserTwo = CreateUser(5, 995);
        var settlement = TournamentSettlement.CreateSingleton();

        var result = await RecordMatchResultHandler.Handle(
            new RecordMatchResultCommand(match.Id, MatchBetSelection.Home),
            new StubMatchRepository(match),
            new StubMatchBetRepository(
                CreateBet(winnerOne, match, MatchBetSelection.Home),
                CreateBet(winnerTwo, match, MatchBetSelection.Home),
                CreateBet(winnerThree, match, MatchBetSelection.Home),
                CreateBet(loserOne, match, MatchBetSelection.Away),
                CreateBet(loserTwo, match, MatchBetSelection.Away)),
            new StubTournamentSettlementRepository(settlement),
            new StubUserRepository(winnerOne, winnerTwo, winnerThree, loserOne, loserTwo),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1003.33m, winnerOne.CurrentBalanceCc);
        Assert.Equal(1003.33m, winnerTwo.CurrentBalanceCc);
        Assert.Equal(1003.33m, winnerThree.CurrentBalanceCc);
        Assert.Equal(995m, loserOne.CurrentBalanceCc);
        Assert.Equal(995m, loserTwo.CurrentBalanceCc);
        Assert.Equal(0.01m, settlement.ChampionJackpotCc);
        Assert.Equal(0.01m, result.Value!.ChampionJackpotContributionCc);
    }

    [Fact]
    public async Task Handle_Returns_Stake_When_All_Bettors_Are_Correct()
    {
        var match = CreateClosedMatch();
        var userOne = CreateUser(1, 995);
        var userTwo = CreateUser(2, 995);
        var settlement = TournamentSettlement.CreateSingleton();

        var result = await RecordMatchResultHandler.Handle(
            new RecordMatchResultCommand(match.Id, MatchBetSelection.Draw),
            new StubMatchRepository(match),
            new StubMatchBetRepository(
                CreateBet(userOne, match, MatchBetSelection.Draw),
                CreateBet(userTwo, match, MatchBetSelection.Draw)),
            new StubTournamentSettlementRepository(settlement),
            new StubUserRepository(userOne, userTwo),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1000, userOne.CurrentBalanceCc);
        Assert.Equal(1000, userTwo.CurrentBalanceCc);
        Assert.Equal(0, settlement.ChampionJackpotCc);
    }

    [Fact]
    public async Task Handle_Refunds_Half_And_Jackpots_Residual_When_Nobody_Is_Correct()
    {
        var match = CreateClosedMatch();
        var userOne = CreateUser(1, 995);
        var userTwo = CreateUser(2, 995);
        var settlement = TournamentSettlement.CreateSingleton();

        var result = await RecordMatchResultHandler.Handle(
            new RecordMatchResultCommand(match.Id, MatchBetSelection.Home),
            new StubMatchRepository(match),
            new StubMatchBetRepository(
                CreateBet(userOne, match, MatchBetSelection.Draw),
                CreateBet(userTwo, match, MatchBetSelection.Away)),
            new StubTournamentSettlementRepository(settlement),
            new StubUserRepository(userOne, userTwo),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(997.50m, userOne.CurrentBalanceCc);
        Assert.Equal(997.50m, userTwo.CurrentBalanceCc);
        Assert.Equal(5m, settlement.ChampionJackpotCc);
        Assert.Equal(5m, result.Value!.ChampionJackpotContributionCc);
    }

    [Fact]
    public async Task Handle_Is_Idempotent_When_Same_Result_Is_Resubmitted()
    {
        var match = CreateClosedMatch();
        var winner = CreateUser(1, 995);
        var loser = CreateUser(2, 995);
        var settlement = TournamentSettlement.CreateSingleton();
        var userRepository = new StubUserRepository(winner, loser);
        var matchBetRepository = new StubMatchBetRepository(
            CreateBet(winner, match, MatchBetSelection.Home),
            CreateBet(loser, match, MatchBetSelection.Away));

        var first = await RecordMatchResultHandler.Handle(
            new RecordMatchResultCommand(match.Id, MatchBetSelection.Home),
            new StubMatchRepository(match),
            matchBetRepository,
            new StubTournamentSettlementRepository(settlement),
            userRepository,
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        var winnerBalanceAfterFirst = winner.CurrentBalanceCc;
        var loserBalanceAfterFirst = loser.CurrentBalanceCc;
        var jackpotAfterFirst = settlement.ChampionJackpotCc;

        var second = await RecordMatchResultHandler.Handle(
            new RecordMatchResultCommand(match.Id, MatchBetSelection.Home),
            new StubMatchRepository(match),
            matchBetRepository,
            new StubTournamentSettlementRepository(settlement),
            userRepository,
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.True(second.Value!.WasAlreadySettled);
        Assert.Equal(winnerBalanceAfterFirst, winner.CurrentBalanceCc);
        Assert.Equal(loserBalanceAfterFirst, loser.CurrentBalanceCc);
        Assert.Equal(jackpotAfterFirst, settlement.ChampionJackpotCc);
        Assert.Equal(1, userRepository.SaveCalls);
    }

    private static Match CreateClosedMatch()
    {
        var match = Match.Create(MatchPhase.GroupStage, "Argentina", "Japan", DateTime.UtcNow.AddMinutes(-10), "MetLife Stadium");
        SetEntityId(match, 20);
        return match;
    }

    private static Match CreateOpenMatch()
    {
        var match = Match.Create(MatchPhase.GroupStage, "Argentina", "Japan", DateTime.UtcNow.AddMinutes(30), "MetLife Stadium");
        SetEntityId(match, 20);
        return match;
    }

    private static User CreateUser(int id, int balanceCc)
    {
        var user = User.Create($"google-{id}", $"user{id}@example.com", $"User {id}");
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

        public Task<IReadOnlyList<Match>> ListPendingResultSettlementAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Match>>(matches.Where(match => match.StartsAtUtc <= nowUtc).ToArray());
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
            throw new NotSupportedException();
        }

        public Task AddAsync(MatchBet matchBet, CancellationToken cancellationToken = default)
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
            throw new NotSupportedException();
        }
    }

    private sealed class StubUserRepository(params User[] users) : IUserRepository
    {
        public int SaveCalls { get; private set; }

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
            SaveCalls++;
            return Task.CompletedTask;
        }
    }
}
