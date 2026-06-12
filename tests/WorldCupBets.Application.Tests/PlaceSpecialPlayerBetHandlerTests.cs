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
            new PlaceSpecialPlayerBetCommand(user.Id, TournamentPickCategory.BestPlayer, "Lionel Messi", "34146370"),
            new StubUserRepository(user),
            new StubMatchRepository(new DateTime(2026, 6, 28, 18, 0, 0, DateTimeKind.Utc)),
            new StubTournamentPickRepository(),
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
    public async Task Handle_Changes_Existing_Special_Player_Selection_Without_Charging_Again()
    {
        var user = User.Create("google-1", "ada@example.com", "Ada");
        SetEntityId(user, 10);
        user.DeductBalance(50);
        var placedAtUtc = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var existingBet = TournamentPick.CreatePlayer(user.Id, TournamentPickCategory.TopScorer, "Kylian Mbappe", "34161330", 50, placedAtUtc);

        var result = await PlaceSpecialPlayerBetHandler.Handle(
            new PlaceSpecialPlayerBetCommand(user.Id, TournamentPickCategory.TopScorer, "Erling Haaland", "34161052"),
            new StubUserRepository(user),
            new StubMatchRepository(new DateTime(2026, 6, 28, 18, 0, 0, DateTimeKind.Utc)),
            new StubTournamentPickRepository(existingBet),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("TopScorer", result.Value!.Category);
        Assert.Equal("Erling Haaland", result.Value.PlayerName);
        Assert.Equal("34161052", result.Value.ExternalPlayerId);
        Assert.Equal("Erling Haaland", existingBet.SelectedText);
        Assert.Equal("34161052", existingBet.ExternalId);
        Assert.Equal(50, result.Value.StakeAmountCc);
        Assert.Equal(placedAtUtc, result.Value.PlacedAtUtc);
        Assert.Equal(950, user.CurrentBalanceCc);
        Assert.Equal(950, result.Value.RemainingBalanceCc);
    }

    [Fact]
    public async Task Handle_Rejects_Special_Player_Bet_When_Betting_Is_Closed()
    {
        var user = User.Create("google-1", "ada@example.com", "Ada");
        SetEntityId(user, 10);

        var result = await PlaceSpecialPlayerBetHandler.Handle(
            new PlaceSpecialPlayerBetCommand(user.Id, TournamentPickCategory.BestPlayer, "Lionel Messi", "34146370"),
            new StubUserRepository(user),
            new StubMatchRepository(DateTime.UtcNow.AddMinutes(-1)),
            new StubTournamentPickRepository(),
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
            new PlaceSpecialPlayerBetCommand(user.Id, TournamentPickCategory.BestPlayer, "Li", null),
            new StubUserRepository(user),
            new StubMatchRepository(new DateTime(2026, 6, 28, 18, 0, 0, DateTimeKind.Utc)),
            new StubTournamentPickRepository(),
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

        public Task<IReadOnlyList<Match>> ListPendingResultSettlementAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Match>>([]);
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

    [Fact]
    public async Task Handle_Rejects_Champion_Category_For_Player_Bets()
    {
        var user = User.Create("google-1", "ada@example.com", "Ada");
        SetEntityId(user, 10);

        var result = await PlaceSpecialPlayerBetHandler.Handle(
            new PlaceSpecialPlayerBetCommand(user.Id, TournamentPickCategory.Champion, "Argentina", null),
            new StubUserRepository(user),
            new StubMatchRepository(new DateTime(2026, 6, 28, 18, 0, 0, DateTimeKind.Utc)),
            new StubTournamentPickRepository(),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("bets.invalid_player_category", result.Error?.Code);
        Assert.Equal(1000, user.CurrentBalanceCc);
    }

    [Fact]
    public async Task Market_Maps_Player_Tournament_Picks_To_Existing_Player_Bet_Dtos()
    {
        var user = User.Create("google-1", "ada@example.com", "Ada");
        SetEntityId(user, 10);

        var result = await GetSpecialBetMarketHandler.Handle(
            new GetSpecialBetMarketQuery(user.Id),
            new StubMatchRepository(new DateTime(2026, 6, 28, 18, 0, 0, DateTimeKind.Utc)),
            new StubTournamentPickRepository(
                TournamentPick.CreatePlayer(user.Id, TournamentPickCategory.BestPlayer, "Lionel Messi", "34146370", 50, DateTime.UtcNow),
                TournamentPick.CreatePlayer(user.Id, TournamentPickCategory.TopScorer, "Kylian Mbappe", null, 50, DateTime.UtcNow)),
            new StubExternalFootballPlayerRepository(new Dictionary<string, string?> { ["34146370"] = "https://example.com/messi.png" }),
            new StubPlayerSquadProvider(),
            CancellationToken.None);

        Assert.Collection(result.PlayerBets,
            bet =>
            {
                Assert.Equal("BestPlayer", bet.Category);
                Assert.Equal("Lionel Messi", bet.PlayerName);
                Assert.Equal("34146370", bet.ExternalPlayerId);
                Assert.Equal("https://example.com/messi.png", bet.PlayerPhotoUrl);
            },
            bet =>
            {
                Assert.Equal("TopScorer", bet.Category);
                Assert.Equal("Kylian Mbappe", bet.PlayerName);
                Assert.Null(bet.ExternalPlayerId);
                Assert.Null(bet.PlayerPhotoUrl);
            });
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

    private sealed class StubExternalFootballPlayerRepository(IReadOnlyDictionary<string, string?> photoUrlsByExternalId) : IExternalFootballPlayerRepository
    {
        public Task ReplacePlayersAsync(string providerName, IReadOnlyList<ExternalFootballPlayerDto> players, DateTime syncedAtUtc, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<ExternalFootballPlayerDto>> SearchAsync(string providerName, string normalizedQuery, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyDictionary<string, string>> GetTeamIdMapAsync(string providerName, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyDictionary<string, string?>> GetPhotoUrlsByExternalIdsAsync(string providerName, IReadOnlyCollection<string> externalIds, CancellationToken cancellationToken = default)
        {
            var matches = photoUrlsByExternalId
                .Where(entry => externalIds.Contains(entry.Key, StringComparer.OrdinalIgnoreCase))
                .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);

            return Task.FromResult<IReadOnlyDictionary<string, string?>>(matches);
        }
    }

    private sealed class StubPlayerSquadProvider : IPlayerSquadProvider
    {
        public string ProviderName => "api-sports";

        public Task<string?> ResolveTeamIdAsync(string teamName, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<PlayerSquadMemberDto>> GetSquadAsync(string teamExternalId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
