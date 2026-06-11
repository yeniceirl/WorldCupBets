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
            new StubTournamentPickRepository(),
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
    public async Task Handle_Changes_Existing_Champion_Selection_Without_Charging_Again()
    {
        var user = User.Create("google-1", "ada@example.com", "Ada");
        SetEntityId(user, 10);
        user.DeductBalance(50);
        var placedAtUtc = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var existingBet = TournamentPick.CreateChampion(user.Id, "Argentina", 50, placedAtUtc);

        var result = await PlaceChampionBetHandler.Handle(
            new PlaceChampionBetCommand(user.Id, "Japan"),
            new StubUserRepository(user),
            new StubMatchRepository(["Argentina", "Japan"], new DateTime(2026, 6, 30, 18, 0, 0, DateTimeKind.Utc)),
            new StubTournamentPickRepository(existingBet),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Japan", result.Value!.TeamName);
        Assert.Equal("Japan", existingBet.SelectedText);
        Assert.Equal(50, result.Value.StakeAmountCc);
        Assert.Equal(placedAtUtc, result.Value.PlacedAtUtc);
        Assert.Equal(950, user.CurrentBalanceCc);
        Assert.Equal(950, result.Value.RemainingBalanceCc);
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
            new StubTournamentPickRepository(),
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
            new StubTournamentPickRepository(),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(User.DeadRescueAmountCc, user.CurrentBalanceCc);
        Assert.Equal(1, user.RescueCount);
        Assert.Equal(User.DeadRescueAmountCc, user.RescueDebtCc);
        Assert.Equal(User.DeadRescueAmountCc, result.Value!.RemainingBalanceCc);
    }

    [Fact]
    public async Task Market_Maps_Current_Champion_Pick_Selected_Text_To_Team_Name()
    {
        var user = User.Create("google-1", "ada@example.com", "Ada");
        SetEntityId(user, 10);

        var result = await GetChampionBetMarketHandler.Handle(
            new GetChampionBetMarketQuery(user.Id),
            new StubMatchRepository(["Argentina", "Japan"], new DateTime(2026, 6, 30, 18, 0, 0, DateTimeKind.Utc)),
            new StubTournamentPickRepository(TournamentPick.CreateChampion(user.Id, "Argentina", 50, DateTime.UtcNow)),
            new StubTournamentSettlementRepository(),
            new StubExternalFootballDataRepository(new ExternalFootballSnapshot(
                [new ExternalFootballTeamDto("ext-arg", "Argentina", "ARG", "AR", "A", "https://example.com/argentina-flag.png")],
                [],
                [],
                [],
                DateTime.UtcNow)),
            new StubFootballDataProvider(),
            CancellationToken.None);

        Assert.Equal("Argentina", result.CurrentUserChampionTeamName);
        Assert.Equal("https://example.com/argentina-flag.png", result.CurrentUserChampionTeamFlagUrl);
        Assert.Equal(["Argentina", "Japan"], result.TeamOptions);
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

    private sealed class StubTournamentSettlementRepository : ITournamentSettlementRepository
    {
        public Task<TournamentSettlement> GetOrCreateSingletonAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> IsChampionSettledAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }
    }

    private sealed class StubTournamentPickRepository(params TournamentPick[] seeded) : ITournamentPickRepository
    {
        private readonly List<TournamentPick> tournamentPicks = [.. seeded];

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
            throw new NotSupportedException();
        }

        public Task<IReadOnlyDictionary<int, decimal>> ListStakeAmountsByUserAsync(IReadOnlyCollection<TournamentPickCategory> categories, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task AddAsync(TournamentPick pick, CancellationToken cancellationToken = default)
        {
            tournamentPicks.Add(pick);
            return Task.CompletedTask;
        }
    }

    private sealed class StubExternalFootballDataRepository(ExternalFootballSnapshot? snapshot) : IExternalFootballDataRepository
    {
        public Task ReplaceSnapshotAsync(string providerName, ExternalFootballSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<ExternalFootballSnapshot?> GetSnapshotAsync(string providerName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(snapshot);
        }
    }

    private sealed class StubFootballDataProvider : IFootballDataProvider
    {
        public string ProviderName => "worldcup26";

        public Task<ExternalFootballSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
