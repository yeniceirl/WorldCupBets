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
            new StubTournamentPickRepository(
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
            new StubTournamentPickRepository(
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
        var winnerThree = CreateUser(3, 950);
        var loser = CreateUser(4, 950);
        var settlement = TournamentSettlement.CreateSingleton();
        settlement.AddChampionJackpot(20);

        var result = await SettleChampionHandler.Handle(
            new SettleChampionCommand("Argentina"),
            new StubTournamentPickRepository(
                CreateBet(winnerOne, "Argentina"),
                CreateBet(winnerTwo, "Argentina"),
                CreateBet(winnerThree, "Argentina"),
                CreateBet(loser, "Japan")),
            new StubTournamentSettlementRepository(settlement),
            new StubUserRepository(winnerOne, winnerTwo, winnerThree, loser),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1023.33m, winnerOne.CurrentBalanceCc);
        Assert.Equal(1023.33m, winnerTwo.CurrentBalanceCc);
        Assert.Equal(1023.33m, winnerThree.CurrentBalanceCc);
        Assert.Equal(0.01m, settlement.UndistributedJackpotCc);
        Assert.Equal(0.01m, result.Value!.UndistributedJackpotCc);
    }

    [Fact]
    public async Task Handle_Is_Idempotent_When_Same_Champion_Is_Resubmitted()
    {
        var winner = CreateUser(1, 950);
        var loser = CreateUser(2, 950);
        var settlement = TournamentSettlement.CreateSingleton();
        settlement.AddChampionJackpot(20);
        var userRepository = new StubUserRepository(winner, loser);
        var championBetRepository = new StubTournamentPickRepository(
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

    [Fact]
    public async Task Handle_Settles_Only_Champion_Tournament_Picks()
    {
        var championWinner = CreateUser(1, 950);
        var championLoser = CreateUser(2, 950);
        var playerPickUser = CreateUser(3, 950);
        var settlement = TournamentSettlement.CreateSingleton();

        var result = await SettleChampionHandler.Handle(
            new SettleChampionCommand("Argentina"),
            new StubTournamentPickRepository(
                CreateChampionPick(championWinner, "Argentina"),
                CreateChampionPick(championLoser, "Japan"),
                CreatePlayerPick(playerPickUser, TournamentPickCategory.BestPlayer, "Lionel Messi"),
                CreatePlayerPick(playerPickUser, TournamentPickCategory.TopScorer, "Kylian Mbappe")),
            new StubTournamentSettlementRepository(settlement),
            new StubUserRepository(championWinner, championLoser, playerPickUser),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1050, championWinner.CurrentBalanceCc);
        Assert.Equal(950, championLoser.CurrentBalanceCc);
        Assert.Equal(950, playerPickUser.CurrentBalanceCc);
        Assert.Equal(1, result.Value!.WinnersCount);
        Assert.Equal(1, result.Value.LosersCount);
    }

    private static User CreateUser(int id, decimal balanceCc)
    {
        var user = User.Create($"google-{id}", $"user{id}@example.com", $"User {id}");
        SetEntityId(user, id);
        SetProperty(user, nameof(User.CurrentBalanceCc), balanceCc);
        return user;
    }

    private static TournamentPick CreateBet(User user, string teamName)
    {
        return CreateChampionPick(user, teamName);
    }

    private static TournamentPick CreateChampionPick(User user, string teamName)
    {
        var bet = TournamentPick.CreateChampion(user.Id, teamName, PlaceChampionBetHandler.ChampionBetStakeAmountCc, DateTime.UtcNow.AddDays(-1));
        SetProperty(bet, nameof(TournamentPick.User), user);
        return bet;
    }

    private static TournamentPick CreatePlayerPick(User user, TournamentPickCategory category, string playerName)
    {
        var bet = TournamentPick.CreatePlayer(user.Id, category, playerName, null, PlaceSpecialPlayerBetHandler.SpecialPlayerBetStakeAmountCc, DateTime.UtcNow.AddDays(-1));
        SetProperty(bet, nameof(TournamentPick.User), user);
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

    private sealed class StubTournamentPickRepository(params TournamentPick[] tournamentPicks) : ITournamentPickRepository
    {
        public Task<TournamentPick?> GetByUserAndCategoryAsync(int userId, TournamentPickCategory category, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(tournamentPicks.SingleOrDefault(tournamentPick => tournamentPick.UserId == userId && tournamentPick.Category == category));
        }

        public Task<TournamentPick?> GetTrackedByUserAndCategoryAsync(int userId, TournamentPickCategory category, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(tournamentPicks.SingleOrDefault(tournamentPick => tournamentPick.UserId == userId && tournamentPick.Category == category));
        }

        public Task<IReadOnlyList<TournamentPick>> ListByUserAndCategoriesAsync(int userId, IReadOnlyCollection<TournamentPickCategory> categories, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TournamentPick>>(tournamentPicks.Where(tournamentPick => tournamentPick.UserId == userId && categories.Contains(tournamentPick.Category)).ToArray());
        }

        public Task<IReadOnlyList<TournamentPick>> ListChampionForSettlementAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TournamentPick>>(tournamentPicks.Where(tournamentPick => tournamentPick.Category == TournamentPickCategory.Champion).ToArray());
        }

        public Task<IReadOnlyDictionary<int, decimal>> ListStakeAmountsByUserAsync(IReadOnlyCollection<TournamentPickCategory> categories, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task AddAsync(TournamentPick pick, CancellationToken cancellationToken = default)
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
