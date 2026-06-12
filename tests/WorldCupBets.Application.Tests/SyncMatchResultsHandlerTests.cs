using System.Reflection;
using WorldCupBets.Application.Abstractions;
using WorldCupBets.Application.Features.Matches;
using WorldCupBets.Domain.Common;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;
using Xunit;

namespace WorldCupBets.Application.Tests;

public sealed class SyncMatchResultsHandlerTests
{
    [Fact]
    public async Task Handle_Defers_When_WorldCup26_Does_Not_Flag_Finished()
    {
        var match = CreateClosedMatch();
        SetProperty(match, nameof(Match.SourceProvider), "worldcup26");
        SetProperty(match, nameof(Match.SourceMatchId), "fixture-1");
        var snapshot = CreateSnapshot(isFinished: false, homeScore: null, awayScore: null);

        var result = await SyncMatchResultsHandler.Handle(
            new SyncMatchResultsCommand(match.Id),
            new StubFootballDataProvider(snapshot),
            new StubOfficialMatchResultProvider(null),
            new StubMatchRepository(match),
            new StubMatchBetRepository(),
            new StubTournamentSettlementRepository(TournamentSettlement.CreateSingleton()),
            new StubUserRepository(),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("deferred", result.Items[0].Status);
        Assert.Null(match.OfficialResult);
        Assert.Null(match.SettledAtUtc);
    }

    [Fact]
    public async Task Handle_Confirms_Result_And_Settles_Match_Once()
    {
        var match = CreateClosedMatch();
        SetProperty(match, nameof(Match.SourceProvider), "worldcup26");
        SetProperty(match, nameof(Match.SourceMatchId), "fixture-1");
        var winner = CreateUser(1, 995m);
        var loser = CreateUser(2, 995m);
        var betRepository = new StubMatchBetRepository(
            CreateBet(winner, match, MatchBetSelection.Home),
            CreateBet(loser, match, MatchBetSelection.Away));
        var settlement = TournamentSettlement.CreateSingleton();
        var snapshot = CreateSnapshot(isFinished: true, homeScore: 2, awayScore: 1);

        var result = await SyncMatchResultsHandler.Handle(
            new SyncMatchResultsCommand(match.Id),
            new StubFootballDataProvider(snapshot),
            new StubOfficialMatchResultProvider(new OfficialMatchResultConfirmation("9001", DateTime.UtcNow, 2, 1)),
            new StubMatchRepository(match),
            betRepository,
            new StubTournamentSettlementRepository(settlement),
            new StubUserRepository(winner, loser),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("confirmed", result.Items[0].Status);
        Assert.Equal(MatchBetSelection.Home, match.OfficialResult);
        Assert.NotNull(match.SettledAtUtc);
        Assert.Equal(1005m, winner.CurrentBalanceCc);
        Assert.Equal(995m, loser.CurrentBalanceCc);
    }

    private static Match CreateClosedMatch()
    {
        var match = Match.Create(MatchPhase.GroupStage, "Mexico", "South Africa", DateTime.UtcNow.AddMinutes(-30), "Estadio Azteca");
        SetEntityId(match, 50);
        return match;
    }

    private static User CreateUser(int id, decimal balanceCc)
    {
        var user = User.Create($"google-{id}", $"user{id}@example.com", $"User {id}");
        SetEntityId(user, id);
        SetProperty(user, nameof(User.CurrentBalanceCc), balanceCc);
        return user;
    }

    private static MatchBet CreateBet(User user, Match match, MatchBetSelection selection)
    {
        var bet = MatchBet.Create(user.Id, match.Id, selection, match.GetStakeAmountCc(), DateTime.UtcNow.AddHours(-1));
        SetProperty(bet, nameof(MatchBet.User), user);
        SetProperty(bet, nameof(MatchBet.Match), match);
        return bet;
    }

    private static ExternalFootballSnapshot CreateSnapshot(bool isFinished, int? homeScore, int? awayScore)
    {
        return new ExternalFootballSnapshot([], [], [],
        [
            new ExternalFootballMatchDto(
                "fixture-1",
                "home-1",
                "away-1",
                "Mexico",
                "South Africa",
                null,
                null,
                "A",
                "1",
                "06/11/2026 13:00",
                "stadium-1",
                isFinished,
                isFinished ? "90" : "0",
                "group",
                homeScore,
                awayScore)
        ], DateTime.UtcNow);
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

    private sealed class StubFootballDataProvider(ExternalFootballSnapshot snapshot) : IFootballDataProvider
    {
        public string ProviderName => "worldcup26";

        public Task<ExternalFootballSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(snapshot);
        }
    }

    private sealed class StubOfficialMatchResultProvider(OfficialMatchResultConfirmation? confirmation) : IOfficialMatchResultProvider
    {
        public string ProviderName => "api-sports";

        public Task<OfficialMatchResultConfirmation?> TryConfirmAsync(OfficialMatchResultLookup lookup, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(confirmation);
        }
    }

    private sealed class StubMatchRepository(params Match[] matches) : IMatchRepository
    {
        public Task<IReadOnlyList<Match>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Match>>(matches);

        public Task<IReadOnlyList<Match>> ListGroupStageFixturesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Match>>(matches.Where(match => match.Phase == MatchPhase.GroupStage).ToArray());

        public Task<IReadOnlySet<int>> ListMatchIdsWithBetsAsync(IEnumerable<int> matchIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlySet<int>>(new HashSet<int>());

        public Task<Match?> GetByIdAsync(int matchId, CancellationToken cancellationToken = default) => Task.FromResult(matches.SingleOrDefault(match => match.Id == matchId));

        public Task<Match?> GetByIdForSettlementAsync(int matchId, CancellationToken cancellationToken = default) => Task.FromResult(matches.SingleOrDefault(match => match.Id == matchId));

        public Task<IReadOnlyList<Match>> ListPendingResultSettlementAsync(DateTime nowUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Match>>(matches.Where(match => match.StartsAtUtc <= nowUtc).ToArray());

        public Task<IReadOnlyList<string>> ListTeamNamesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<string>>(matches.SelectMany(match => new[] { match.HomeTeamName, match.AwayTeamName }).Distinct().ToArray());

        public Task<DateTime?> GetChampionBettingClosesAtUtcAsync(CancellationToken cancellationToken = default) => Task.FromResult<DateTime?>(null);

        public Task AddAsync(Match match, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubMatchBetRepository(params MatchBet[] matchBets) : IMatchBetRepository
    {
        public Task<bool> ExistsForUserAndMatchAsync(int userId, int matchId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<MatchBet?> GetByUserAndMatchAsync(int userId, int matchId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<MatchBet>> ListByUserAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<MatchBet>> ListByMatchForSettlementAsync(int matchId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MatchBet>>(matchBets.Where(matchBet => matchBet.MatchId == matchId).ToArray());

        public Task<IReadOnlyDictionary<int, decimal>> ListPendingStakeAmountsByUserAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task AddAsync(MatchBet matchBet, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubTournamentSettlementRepository(TournamentSettlement settlement) : ITournamentSettlementRepository
    {
        public Task<TournamentSettlement> GetOrCreateSingletonAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(settlement);
        }

        public Task<bool> IsChampionSettledAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }
    }

    private sealed class StubUserRepository(params User[] users) : IUserRepository
    {
        public Task<User?> GetByGoogleSubjectWithRolesAsync(string googleSubject, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default) => Task.FromResult(users.SingleOrDefault(user => user.Id == userId));

        public Task<IReadOnlyList<User>> ListLeaderboardAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NoopApplicationTransactionFactory : IApplicationTransactionFactory
    {
        public Task<IApplicationTransaction> BeginSerializableAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IApplicationTransaction>(new NoopApplicationTransaction());
        }
    }

    private sealed class NoopApplicationTransaction : IApplicationTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
