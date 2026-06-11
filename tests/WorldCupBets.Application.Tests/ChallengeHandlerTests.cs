using System.Reflection;
using WorldCupBets.Application.Abstractions;
using WorldCupBets.Application.Features.Challenges;
using WorldCupBets.Domain.Common;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;
using Xunit;

namespace WorldCupBets.Application.Tests;

public sealed class ChallengeHandlerTests
{
    [Fact]
    public async Task Create_Deducts_Creator_Balance_And_Stores_Open_Challenge()
    {
        var user = CreateUser(1, "Creator");
        var match = CreateMatch(10);
        var challenges = new StubChallengeRepository();

        var result = await CreateChallengeHandler.Handle(
            new CreateChallengeCommand(user.Id, match.Id, "Top scorer gets two", 25),
            new StubUserRepository(user),
            new StubMatchRepository(match),
            challenges,
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(975, user.CurrentBalanceCc);
        Assert.Single(challenges.Stored);
        Assert.Equal("Open", result.Value!.Challenge.Status);
    }

    [Fact]
    public async Task Create_Rejects_Closed_Match_Betting_Window_Without_Escrow()
    {
        var user = CreateUser(1, "Creator");
        var match = CreateMatch(10, DateTime.UtcNow.AddMinutes(-6));
        var challenges = new StubChallengeRepository();

        var result = await CreateChallengeHandler.Handle(
            new CreateChallengeCommand(user.Id, match.Id, "Top scorer gets two", 25),
            new StubUserRepository(user),
            new StubMatchRepository(match),
            challenges,
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("challenges.window_closed", result.Error?.Code);
        Assert.Equal(User.InitialBalanceCc, user.CurrentBalanceCc);
        Assert.Empty(challenges.Stored);
    }

    [Fact]
    public async Task Create_Rejects_Invalid_Match_Without_Escrow()
    {
        var user = CreateUser(1, "Creator");
        var challenges = new StubChallengeRepository();

        var result = await CreateChallengeHandler.Handle(
            new CreateChallengeCommand(user.Id, 404, "Claim", 25),
            new StubUserRepository(user),
            new StubMatchRepository(),
            challenges,
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("challenges.match_not_found", result.Error?.Code);
        Assert.Equal(User.InitialBalanceCc, user.CurrentBalanceCc);
        Assert.Empty(challenges.Stored);
    }

    [Fact]
    public async Task Create_Rejects_Invalid_Text_Without_Escrow()
    {
        var user = CreateUser(1, "Creator");
        var match = CreateMatch(10);
        var challenges = new StubChallengeRepository();

        var result = await CreateChallengeHandler.Handle(
            new CreateChallengeCommand(user.Id, match.Id, " ", 25),
            new StubUserRepository(user),
            new StubMatchRepository(match),
            challenges,
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("challenges.invalid_payload", result.Error?.Code);
        Assert.Equal(User.InitialBalanceCc, user.CurrentBalanceCc);
        Assert.Empty(challenges.Stored);
    }

    [Fact]
    public async Task Create_Rejects_Insufficient_Balance_Without_Escrow()
    {
        var user = CreateUser(1, "Creator", balanceCc: 10);
        var match = CreateMatch(10);
        var challenges = new StubChallengeRepository();

        var result = await CreateChallengeHandler.Handle(
            new CreateChallengeCommand(user.Id, match.Id, "Claim", 25),
            new StubUserRepository(user),
            new StubMatchRepository(match),
            challenges,
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("challenges.insufficient_balance", result.Error?.Code);
        Assert.Equal(10, user.CurrentBalanceCc);
        Assert.Empty(challenges.Stored);
    }

    [Fact]
    public async Task Accept_Rejects_Self_Accept_Without_Deducting_Balance()
    {
        var creator = CreateUser(1, "Creator");
        var challenge = MatchChallenge.Create(creator.Id, 10, "Claim", "For", "Against", 25, DateTime.UtcNow);
        SetEntityId(challenge, 20);

        var result = await AcceptChallengeHandler.Handle(
            new AcceptChallengeCommand(challenge.Id, creator.Id),
            new StubUserRepository(creator),
            new StubMatchRepository(CreateMatch(10)),
            new StubChallengeRepository(challenge),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("challenges.self_accept", result.Error?.Code);
        Assert.Equal(User.InitialBalanceCc, creator.CurrentBalanceCc);
        Assert.Equal(MatchChallengeStatus.Open, challenge.Status);
    }

    [Fact]
    public async Task Accept_Deducts_Taker_Balance_And_Matches_Challenge()
    {
        var creator = CreateUser(1, "Creator");
        var taker = CreateUser(2, "Taker");
        var challenge = MatchChallenge.Create(creator.Id, 10, "Claim", "For", "Against", 30, DateTime.UtcNow);
        SetEntityId(challenge, 20);

        var result = await AcceptChallengeHandler.Handle(
            new AcceptChallengeCommand(challenge.Id, taker.Id),
            new StubUserRepository(creator, taker),
            new StubMatchRepository(CreateMatch(10)),
            new StubChallengeRepository(challenge),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(970, taker.CurrentBalanceCc);
        Assert.Equal(MatchChallengeStatus.Matched, challenge.Status);
        Assert.Equal(taker.Id, challenge.TakerPosition?.UserId);
    }

    [Fact]
    public async Task Accept_Rejects_Already_Matched_Challenge_Without_Extra_Escrow()
    {
        var creator = CreateUser(1, "Creator");
        var taker = CreateUser(2, "Taker");
        var secondTaker = CreateUser(3, "Second Taker");
        var challenge = MatchChallenge.Create(creator.Id, 10, "Claim", "For", "Against", 30, DateTime.UtcNow);
        SetEntityId(challenge, 20);
        challenge.Accept(taker.Id, DateTime.UtcNow);

        var result = await AcceptChallengeHandler.Handle(
            new AcceptChallengeCommand(challenge.Id, secondTaker.Id),
            new StubUserRepository(creator, taker, secondTaker),
            new StubMatchRepository(CreateMatch(10)),
            new StubChallengeRepository(challenge),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("challenges.not_open", result.Error?.Code);
        Assert.Equal(User.InitialBalanceCc, secondTaker.CurrentBalanceCc);
        Assert.Equal(MatchChallengeStatus.Matched, challenge.Status);
        Assert.Equal(2, challenge.Positions.Count);
    }

    [Fact]
    public async Task Accept_Rejects_Terminal_Challenge_Without_Extra_Escrow()
    {
        var creator = CreateUser(1, "Creator");
        var taker = CreateUser(2, "Taker");
        var challenge = MatchChallenge.Create(creator.Id, 10, "Claim", "For", "Against", 30, DateTime.UtcNow);
        SetEntityId(challenge, 20);
        challenge.Void(DateTime.UtcNow);

        var result = await AcceptChallengeHandler.Handle(
            new AcceptChallengeCommand(challenge.Id, taker.Id),
            new StubUserRepository(creator, taker),
            new StubMatchRepository(CreateMatch(10)),
            new StubChallengeRepository(challenge),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("challenges.not_open", result.Error?.Code);
        Assert.Equal(User.InitialBalanceCc, taker.CurrentBalanceCc);
        Assert.Equal(MatchChallengeStatus.Voided, challenge.Status);
        Assert.Single(challenge.Positions);
    }

    [Fact]
    public async Task Accept_Rejects_Closed_Match_Betting_Window_Without_Deducting_Balance()
    {
        var creator = CreateUser(1, "Creator");
        var taker = CreateUser(2, "Taker");
        var challenge = MatchChallenge.Create(creator.Id, 10, "Claim", "For", "Against", 30, DateTime.UtcNow);
        SetEntityId(challenge, 20);

        var result = await AcceptChallengeHandler.Handle(
            new AcceptChallengeCommand(challenge.Id, taker.Id),
            new StubUserRepository(creator, taker),
            new StubMatchRepository(CreateMatch(10, DateTime.UtcNow.AddMinutes(-6))),
            new StubChallengeRepository(challenge),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("challenges.window_closed", result.Error?.Code);
        Assert.Equal(User.InitialBalanceCc, taker.CurrentBalanceCc);
        Assert.Equal(MatchChallengeStatus.Open, challenge.Status);
    }

    [Fact]
    public async Task Settle_Pays_Full_Escrow_To_Winning_Side()
    {
        var creator = CreateUser(1, "Creator");
        var taker = CreateUser(2, "Taker");
        var challenge = MatchChallenge.Create(creator.Id, 10, "Claim", "For", "Against", 10, DateTime.UtcNow);
        SetEntityId(challenge, 20);
        challenge.Accept(taker.Id, DateTime.UtcNow);
        creator.DeductBalance(10);
        taker.DeductBalance(10);

        var result = await SettleChallengeHandler.Handle(
            new SettleChallengeCommand(challenge.Id, MatchChallengeSide.Creator),
            new StubUserRepository(creator, taker),
            new StubChallengeRepository(challenge),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(MatchChallengeStatus.Settled, challenge.Status);
        Assert.Equal(1010, creator.CurrentBalanceCc);
        Assert.Equal(990, taker.CurrentBalanceCc);
    }

    [Fact]
    public async Task Settle_Rejects_Non_Matched_Challenge_Without_Payout()
    {
        var creator = CreateUser(1, "Creator");
        var challenge = MatchChallenge.Create(creator.Id, 10, "Claim", "For", "Against", 10, DateTime.UtcNow);
        SetEntityId(challenge, 20);
        creator.DeductBalance(10);

        var result = await SettleChallengeHandler.Handle(
            new SettleChallengeCommand(challenge.Id, MatchChallengeSide.Creator),
            new StubUserRepository(creator),
            new StubChallengeRepository(challenge),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("challenges.not_matched", result.Error?.Code);
        Assert.Equal(990, creator.CurrentBalanceCc);
        Assert.Equal(MatchChallengeStatus.Open, challenge.Status);
    }

    [Fact]
    public async Task Settle_Rejects_Invalid_Winner_Side_Without_Payout()
    {
        var creator = CreateUser(1, "Creator");
        var taker = CreateUser(2, "Taker");
        var challenge = MatchChallenge.Create(creator.Id, 10, "Claim", "For", "Against", 10, DateTime.UtcNow);
        SetEntityId(challenge, 20);
        challenge.Accept(taker.Id, DateTime.UtcNow);
        creator.DeductBalance(10);
        taker.DeductBalance(10);

        var result = await SettleChallengeHandler.Handle(
            new SettleChallengeCommand(challenge.Id, (MatchChallengeSide)999),
            new StubUserRepository(creator, taker),
            new StubChallengeRepository(challenge),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("challenges.invalid_payload", result.Error?.Code);
        Assert.Equal(990, creator.CurrentBalanceCc);
        Assert.Equal(990, taker.CurrentBalanceCc);
        Assert.Equal(MatchChallengeStatus.Matched, challenge.Status);
    }

    [Fact]
    public async Task Void_Refunds_Open_Challenge_Creator_Escrow()
    {
        var creator = CreateUser(1, "Creator");
        var challenge = MatchChallenge.Create(creator.Id, 10, "Claim", "For", "Against", 10, DateTime.UtcNow);
        SetEntityId(challenge, 20);
        creator.DeductBalance(10);

        var result = await VoidChallengeHandler.Handle(
            new VoidChallengeCommand(challenge.Id),
            new StubUserRepository(creator),
            new StubChallengeRepository(challenge),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(MatchChallengeStatus.Voided, challenge.Status);
        Assert.Equal(User.InitialBalanceCc, creator.CurrentBalanceCc);
    }

    [Fact]
    public async Task Cancel_Refunds_Open_Challenge_For_Creator()
    {
        var creator = CreateUser(1, "Creator");
        var challenge = MatchChallenge.Create(creator.Id, 10, "Claim", "For", "Against", 10, DateTime.UtcNow);
        SetEntityId(challenge, 20);
        creator.DeductBalance(10);

        var result = await CancelChallengeHandler.Handle(
            new CancelChallengeCommand(challenge.Id, creator.Id),
            new StubUserRepository(creator),
            new StubChallengeRepository(challenge),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(MatchChallengeStatus.Voided, challenge.Status);
        Assert.Equal(User.InitialBalanceCc, creator.CurrentBalanceCc);
        Assert.Equal(User.InitialBalanceCc, result.Value!.CurrentBalanceCc);
    }

    [Fact]
    public async Task Cancel_Rejects_Non_Creator_Without_Refund()
    {
        var creator = CreateUser(1, "Creator");
        var otherUser = CreateUser(2, "Other");
        var challenge = MatchChallenge.Create(creator.Id, 10, "Claim", "For", "Against", 10, DateTime.UtcNow);
        SetEntityId(challenge, 20);
        creator.DeductBalance(10);

        var result = await CancelChallengeHandler.Handle(
            new CancelChallengeCommand(challenge.Id, otherUser.Id),
            new StubUserRepository(creator, otherUser),
            new StubChallengeRepository(challenge),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("challenges.not_creator", result.Error?.Code);
        Assert.Equal(990, creator.CurrentBalanceCc);
        Assert.Equal(MatchChallengeStatus.Open, challenge.Status);
    }

    [Fact]
    public async Task Cancel_Rejects_Matched_Challenge_Without_Refund()
    {
        var creator = CreateUser(1, "Creator");
        var taker = CreateUser(2, "Taker");
        var challenge = MatchChallenge.Create(creator.Id, 10, "Claim", "For", "Against", 10, DateTime.UtcNow);
        SetEntityId(challenge, 20);
        challenge.Accept(taker.Id, DateTime.UtcNow);
        creator.DeductBalance(10);

        var result = await CancelChallengeHandler.Handle(
            new CancelChallengeCommand(challenge.Id, creator.Id),
            new StubUserRepository(creator, taker),
            new StubChallengeRepository(challenge),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("challenges.not_open", result.Error?.Code);
        Assert.Equal(990, creator.CurrentBalanceCc);
        Assert.Equal(MatchChallengeStatus.Matched, challenge.Status);
    }

    [Fact]
    public async Task Expire_Refunds_Matched_Challenge_Participants()
    {
        var creator = CreateUser(1, "Creator");
        var taker = CreateUser(2, "Taker");
        var challenge = MatchChallenge.Create(creator.Id, 10, "Claim", "For", "Against", 10, DateTime.UtcNow);
        SetEntityId(challenge, 20);
        challenge.Accept(taker.Id, DateTime.UtcNow);
        creator.DeductBalance(10);
        taker.DeductBalance(10);

        var result = await ExpireChallengeHandler.Handle(
            new ExpireChallengeCommand(challenge.Id),
            new StubUserRepository(creator, taker),
            new StubChallengeRepository(challenge),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(MatchChallengeStatus.Expired, challenge.Status);
        Assert.Equal(User.InitialBalanceCc, creator.CurrentBalanceCc);
        Assert.Equal(User.InitialBalanceCc, taker.CurrentBalanceCc);
    }

    [Fact]
    public async Task Void_Rejects_Terminal_Challenge_Without_Second_Refund()
    {
        var creator = CreateUser(1, "Creator");
        var challenge = MatchChallenge.Create(creator.Id, 10, "Claim", "For", "Against", 10, DateTime.UtcNow);
        SetEntityId(challenge, 20);
        creator.DeductBalance(10);
        challenge.Void(DateTime.UtcNow);
        creator.CreditBalance(10);

        var result = await VoidChallengeHandler.Handle(
            new VoidChallengeCommand(challenge.Id),
            new StubUserRepository(creator),
            new StubChallengeRepository(challenge),
            new NoopApplicationTransactionFactory(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("challenges.terminal", result.Error?.Code);
        Assert.Equal(User.InitialBalanceCc, creator.CurrentBalanceCc);
    }

    private static User CreateUser(int id, string displayName, decimal balanceCc = User.InitialBalanceCc)
    {
        var user = User.Create($"google-{id}", $"user{id}@example.com", displayName);
        SetEntityId(user, id);
        SetProperty(user, nameof(User.CurrentBalanceCc), balanceCc);
        return user;
    }

    private static Match CreateMatch(int id, DateTime? startsAtUtc = null)
    {
        var match = Match.Create(MatchPhase.GroupStage, "Argentina", "Japan", startsAtUtc ?? DateTime.UtcNow.AddDays(1), "MetLife Stadium");
        SetEntityId(match, id);
        return match;
    }

    private static void SetEntityId(Entity entity, int id)
    {
        SetProperty(entity, nameof(Entity.Id), id);
    }

    private static void SetProperty(object target, string propertyName, object? value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? typeof(Entity).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        property!.SetValue(target, value);
    }

    private sealed class StubUserRepository(params User[] users) : IUserRepository
    {
        public Task<User?> GetByGoogleSubjectWithRolesAsync(string googleSubject, CancellationToken cancellationToken = default) =>
            Task.FromResult(users.SingleOrDefault(user => user.GoogleSubject == googleSubject));

        public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(users.SingleOrDefault(user => user.Id == userId));

        public Task<IReadOnlyList<User>> ListLeaderboardAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<User>>(users);

        public Task AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubMatchRepository(params Match[] matches) : IMatchRepository
    {
        public Task<IReadOnlyList<Match>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Match>>(matches);

        public Task<IReadOnlyList<Match>> ListGroupStageFixturesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Match>>(matches);

        public Task<IReadOnlySet<int>> ListMatchIdsWithBetsAsync(IEnumerable<int> matchIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlySet<int>>(new HashSet<int>());

        public Task<Match?> GetByIdAsync(int matchId, CancellationToken cancellationToken = default) => Task.FromResult(matches.SingleOrDefault(match => match.Id == matchId));

        public Task<Match?> GetByIdForSettlementAsync(int matchId, CancellationToken cancellationToken = default) => Task.FromResult(matches.SingleOrDefault(match => match.Id == matchId));

        public Task<IReadOnlyList<string>> ListTeamNamesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<string>>([]);

        public Task<DateTime?> GetChampionBettingClosesAtUtcAsync(CancellationToken cancellationToken = default) => Task.FromResult<DateTime?>(null);

        public Task AddAsync(Match match, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubChallengeRepository(params MatchChallenge[] challenges) : IMatchChallengeRepository
    {
        public List<MatchChallenge> Stored { get; } = [.. challenges];

        public Task<IReadOnlyList<MatchChallenge>> ListByMatchAsync(int matchId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MatchChallenge>>(Stored.Where(challenge => challenge.MatchId == matchId).ToArray());

        public Task<MatchChallenge?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored.SingleOrDefault(challenge => challenge.Id == id));

        public Task<MatchChallenge?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored.SingleOrDefault(challenge => challenge.Id == id));

        public Task<IReadOnlyDictionary<int, decimal>> ListActiveStakeAmountsByUserAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<int, decimal>>(new Dictionary<int, decimal>());

        public Task AddAsync(MatchChallenge matchChallenge, CancellationToken cancellationToken = default)
        {
            Stored.Add(matchChallenge);
            return Task.CompletedTask;
        }
    }

    private sealed class NoopApplicationTransactionFactory : IApplicationTransactionFactory
    {
        public Task<IApplicationTransaction> BeginSerializableAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IApplicationTransaction>(new NoopApplicationTransaction());
    }

    private sealed class NoopApplicationTransaction : IApplicationTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
