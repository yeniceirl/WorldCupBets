using System.Reflection;
using WorldCupBets.Application.Abstractions;
using WorldCupBets.Application.Features.Bets;
using WorldCupBets.Domain.Common;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;
using Xunit;

namespace WorldCupBets.Application.Tests;

public sealed class SettleChampionHandlerTests
{
    [Fact]
    public async Task Handle_Includes_Available_Jackpot_In_Winner_Payout()
    {
        var winner = CreateUser(1, 950);
        var loser = CreateUser(2, 950);
        var settlement = TournamentSettlement.CreateSingleton();
        settlement.AddChampionJackpot(20);

        var result = await SettleChampionHandler.Handle(
            new SettleChampionCommand("Argentina"),
            new StubChampionBetRepository(
                CreateBet(winner, "Argentina"),
                CreateBet(loser, "Japan")),
            new StubTournamentSettlementRepository(settlement),
            new StubUserRepository(winner, loser),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1070, winner.CurrentBalanceCc);
        Assert.Equal(950, loser.CurrentBalanceCc);
        Assert.Equal(20, result.Value!.ChampionJackpotCc);
        Assert.Equal(50, result.Value.LosingStakePoolCc);
        Assert.Equal(70, result.Value.ProfitSharePerWinnerCc);
        Assert.Equal(120, result.Value.TotalPayoutPerWinnerCc);
    }

    [Fact]
    public async Task Handle_Splits_Losing_Stakes_And_Jackpot_Among_Winners()
    {
        var winnerOne = CreateUser(1, 950);
        var winnerTwo = CreateUser(2, 950);
        var loserOne = CreateUser(3, 950);
        var loserTwo = CreateUser(4, 950);
        var settlement = TournamentSettlement.CreateSingleton();
        settlement.AddChampionJackpot(20);

        var result = await SettleChampionHandler.Handle(
            new SettleChampionCommand("Argentina"),
            new StubChampionBetRepository(
                CreateBet(winnerOne, "Argentina"),
                CreateBet(winnerTwo, "Argentina"),
                CreateBet(loserOne, "Japan"),
                CreateBet(loserTwo, "Brazil")),
            new StubTournamentSettlementRepository(settlement),
            new StubUserRepository(winnerOne, winnerTwo, loserOne, loserTwo),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1060, winnerOne.CurrentBalanceCc);
        Assert.Equal(1060, winnerTwo.CurrentBalanceCc);
        Assert.Equal(950, loserOne.CurrentBalanceCc);
        Assert.Equal(950, loserTwo.CurrentBalanceCc);
        Assert.Equal(60, result.Value!.ProfitSharePerWinnerCc);
        Assert.Equal(110, result.Value.TotalPayoutPerWinnerCc);
    }

    [Fact]
    public async Task Handle_Records_Remainder_As_Undistributed_Jackpot()
    {
        var winnerOne = CreateUser(1, 950);
        var winnerTwo = CreateUser(2, 950);
        var loser = CreateUser(3, 950);
        var settlement = TournamentSettlement.CreateSingleton();
        settlement.AddChampionJackpot(21);

        var result = await SettleChampionHandler.Handle(
            new SettleChampionCommand("Argentina"),
            new StubChampionBetRepository(
                CreateBet(winnerOne, "Argentina"),
                CreateBet(winnerTwo, "Argentina"),
                CreateBet(loser, "Japan")),
            new StubTournamentSettlementRepository(settlement),
            new StubUserRepository(winnerOne, winnerTwo, loser),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1035, winnerOne.CurrentBalanceCc);
        Assert.Equal(1035, winnerTwo.CurrentBalanceCc);
        Assert.Equal(1, settlement.UndistributedJackpotCc);
        Assert.Equal(1, result.Value!.UndistributedJackpotCc);
    }

    [Fact]
    public async Task Handle_Is_Idempotent_When_Same_Champion_Is_Resubmitted()
    {
        var winner = CreateUser(1, 950);
        var loser = CreateUser(2, 950);
        var settlement = TournamentSettlement.CreateSingleton();
        settlement.AddChampionJackpot(20);
        var userRepository = new StubUserRepository(winner, loser);
        var championBetRepository = new StubChampionBetRepository(
            CreateBet(winner, "Argentina"),
            CreateBet(loser, "Japan"));

        var first = await SettleChampionHandler.Handle(
            new SettleChampionCommand("Argentina"),
            championBetRepository,
            new StubTournamentSettlementRepository(settlement),
            userRepository,
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        var winnerBalanceAfterFirst = winner.CurrentBalanceCc;
        var loserBalanceAfterFirst = loser.CurrentBalanceCc;
        var jackpotAfterFirst = settlement.ChampionJackpotCc;
        var undistributedAfterFirst = settlement.UndistributedJackpotCc;

        var second = await SettleChampionHandler.Handle(
            new SettleChampionCommand("Argentina"),
            championBetRepository,
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
        Assert.Equal(undistributedAfterFirst, settlement.UndistributedJackpotCc);
        Assert.Equal(1, userRepository.SaveCalls);
    }

    private static User CreateUser(int id, int balanceCc)
    {
        var user = User.Create($"google-{id}", $"user{id}@example.com", $"User {id}");
        SetEntityId(user, id);
        SetProperty(user, nameof(User.CurrentBalanceCc), balanceCc);
        return user;
    }

    private static ChampionBet CreateBet(User user, string teamName)
    {
        var bet = ChampionBet.Create(user.Id, teamName, PlaceChampionBetHandler.ChampionBetStakeAmountCc, DateTime.UtcNow.AddDays(-1));
        SetProperty(bet, nameof(ChampionBet.User), user);
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
        property!.SetValue(target, value);
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
