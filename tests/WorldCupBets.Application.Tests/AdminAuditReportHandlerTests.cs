using WorldCupBets.Application.Abstractions;
using WorldCupBets.Application.Features.Admin;
using WorldCupBets.Domain.Entities;
using Xunit;

namespace WorldCupBets.Application.Tests;

public sealed class AdminAuditReportHandlerTests
{
    [Fact]
    public async Task Handle_Summary_Returns_Derived_Current_State_Totals()
    {
        var repository = new StubAuditReadRepository(
            users:
            [
                new AuditUserReadModel(1, "Winner", "winner@example.com", 1005m, 0, 0m),
                new AuditUserReadModel(2, "Mixed", "mixed@example.com", 900m, 0, 0m)
            ],
            matchBets:
            [
                new AuditMatchBetReadModel(10, 1, MatchBetSelection.Home, 5m, new DateTime(2026, 6, 11, 17, 0, 0, DateTimeKind.Utc), 100, "Mexico", "South Africa", new DateTime(2026, 6, 11, 18, 0, 0, DateTimeKind.Utc), MatchBetSelection.Home, new DateTime(2026, 6, 11, 20, 0, 0, DateTimeKind.Utc)),
                new AuditMatchBetReadModel(11, 99, MatchBetSelection.Away, 5m, new DateTime(2026, 6, 11, 17, 0, 0, DateTimeKind.Utc), 100, "Mexico", "South Africa", new DateTime(2026, 6, 11, 18, 0, 0, DateTimeKind.Utc), MatchBetSelection.Home, new DateTime(2026, 6, 11, 20, 0, 0, DateTimeKind.Utc))
            ],
            challengePositions:
            [
                new AuditChallengePositionReadModel(20, 2, MatchChallengeSide.Creator, 25m, new DateTime(2026, 6, 12, 17, 0, 0, DateTimeKind.Utc), MatchChallengeStatus.Open, null, "Mexico wins", "Yes", "No", "Mexico", "Japan", 1, 25m)
            ],
            tournamentPicks:
            [
                new AuditTournamentPickReadModel(30, 2, TournamentPickCategory.Champion, "Brazil", 50m, new DateTime(2026, 6, 10, 17, 0, 0, DateTimeKind.Utc)),
                new AuditTournamentPickReadModel(31, 2, TournamentPickCategory.TopScorer, "Kylian Mbappe", 50m, new DateTime(2026, 6, 10, 18, 0, 0, DateTimeKind.Utc))
            ],
            settlement: new AuditTournamentSettlementReadModel("Argentina", new DateTime(2026, 7, 19, 1, 0, 0, DateTimeKind.Utc), 0m, 0m));

        var result = await GetAuditBalanceSummaryHandler.Handle(new GetAuditBalanceSummaryQuery(), repository, CancellationToken.None);

        Assert.True(result.Metadata.IsDerivedFromCurrentState);
        Assert.Equal(2, result.Rows.Count);

        var winner = Assert.Single(result.Rows, row => row.UserId == 1);
        Assert.Equal(1005m, winner.AvailableBalanceCc);
        Assert.Equal(0m, winner.PendingTotalCc);
        Assert.Equal(1005m, winner.DerivedTotalBalanceCc);
        Assert.Equal(10m, winner.WonTotalCc);
        Assert.Equal(0m, winner.LostTotalCc);

        var mixed = Assert.Single(result.Rows, row => row.UserId == 2);
        Assert.Equal(900m, mixed.AvailableBalanceCc);
        Assert.Equal(75m, mixed.PendingTotalCc);
        Assert.Equal(975m, mixed.DerivedTotalBalanceCc);
        Assert.Equal(0m, mixed.WonTotalCc);
        Assert.Equal(50m, mixed.LostTotalCc);
    }

    [Fact]
    public async Task Handle_Subledger_Returns_Pending_Reasons_And_Terminal_Statuses()
    {
        var repository = new StubAuditReadRepository(
            users: [new AuditUserReadModel(7, "Ada", "ada@example.com", 950m, 1, 100m)],
            matchBets:
            [
                new AuditMatchBetReadModel(10, 7, MatchBetSelection.Home, 5m, new DateTime(2026, 6, 11, 17, 0, 0, DateTimeKind.Utc), 100, "Mexico", "South Africa", new DateTime(2026, 6, 11, 18, 0, 0, DateTimeKind.Utc), null, null)
            ],
            challengePositions:
            [
                new AuditChallengePositionReadModel(20, 7, MatchChallengeSide.Creator, 25m, new DateTime(2026, 6, 12, 17, 0, 0, DateTimeKind.Utc), MatchChallengeStatus.Expired, null, "Mexico wins", "Yes", "No", "Mexico", "Japan", 1, 25m)
            ],
            tournamentPicks:
            [
                new AuditTournamentPickReadModel(30, 7, TournamentPickCategory.Champion, "Argentina", 50m, new DateTime(2026, 6, 10, 17, 0, 0, DateTimeKind.Utc)),
                new AuditTournamentPickReadModel(31, 7, TournamentPickCategory.BestPlayer, "Lionel Messi", 50m, new DateTime(2026, 6, 10, 18, 0, 0, DateTimeKind.Utc))
            ],
            settlement: null);

        var result = await GetAuditUserSubledgerHandler.Handle(new GetAuditUserSubledgerQuery(7), repository, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(4, result!.Items.Count);

        var match = Assert.Single(result.Items, item => item.SourceType == "match_bet");
        Assert.Equal("pending", match.Status);
        Assert.Equal("Waiting for official match result", match.PendingReason);

        var challenge = Assert.Single(result.Items, item => item.SourceType == "challenge");
        Assert.Equal("refunded", challenge.Status);
        Assert.Null(challenge.PendingReason);
        Assert.Equal(25m, challenge.CreditAmountCc);

        var champion = Assert.Single(result.Items, item => item.SourceType == "tournament_pick" && item.Label == "Champion pick");
        Assert.Equal("pending", champion.Status);
        Assert.Equal("Waiting for champion settlement", champion.PendingReason);

        var special = Assert.Single(result.Items, item => item.SourceType == "tournament_pick" && item.Label == "Best player pick");
        Assert.Equal("pending", special.Status);
        Assert.Equal("Waiting for tournament special settlement", special.PendingReason);
    }

    [Fact]
    public async Task Handle_Subledger_Uses_Full_Match_Pool_For_Winner_Payouts()
    {
        var repository = new StubAuditReadRepository(
            users: [new AuditUserReadModel(7, "Ada", "ada@example.com", 1015m, 0, 0m)],
            matchBets:
            [
                new AuditMatchBetReadModel(10, 7, MatchBetSelection.Draw, 5m, new DateTime(2026, 6, 11, 17, 0, 0, DateTimeKind.Utc), 100, "Canada", "Bosnia and Herzegovina", new DateTime(2026, 6, 11, 18, 0, 0, DateTimeKind.Utc), MatchBetSelection.Draw, new DateTime(2026, 6, 11, 20, 0, 0, DateTimeKind.Utc)),
                new AuditMatchBetReadModel(11, 8, MatchBetSelection.Home, 5m, new DateTime(2026, 6, 11, 17, 0, 0, DateTimeKind.Utc), 100, "Canada", "Bosnia and Herzegovina", new DateTime(2026, 6, 11, 18, 0, 0, DateTimeKind.Utc), MatchBetSelection.Draw, new DateTime(2026, 6, 11, 20, 0, 0, DateTimeKind.Utc)),
                new AuditMatchBetReadModel(12, 9, MatchBetSelection.Away, 5m, new DateTime(2026, 6, 11, 17, 0, 0, DateTimeKind.Utc), 100, "Canada", "Bosnia and Herzegovina", new DateTime(2026, 6, 11, 18, 0, 0, DateTimeKind.Utc), MatchBetSelection.Draw, new DateTime(2026, 6, 11, 20, 0, 0, DateTimeKind.Utc))
            ],
            challengePositions: [],
            tournamentPicks: [],
            settlement: null);

        var result = await GetAuditUserSubledgerHandler.Handle(new GetAuditUserSubledgerQuery(7), repository, CancellationToken.None);

        Assert.NotNull(result);
        var match = Assert.Single(result!.Items, item => item.SourceType == "match_bet");
        Assert.Equal("won", match.Status);
        Assert.Equal(15m, match.CreditAmountCc);
        Assert.Equal(0m, match.LossAmountCc);
        Assert.Equal(0m, match.PendingAmountCc);
    }

    private sealed class StubAuditReadRepository(
        IReadOnlyList<AuditUserReadModel> users,
        IReadOnlyList<AuditMatchBetReadModel> matchBets,
        IReadOnlyList<AuditChallengePositionReadModel> challengePositions,
        IReadOnlyList<AuditTournamentPickReadModel> tournamentPicks,
        AuditTournamentSettlementReadModel? settlement) : IAuditReadRepository
    {
        public Task<IReadOnlyList<AuditUserReadModel>> ListUsersAsync(CancellationToken cancellationToken = default) => Task.FromResult(users);

        public Task<AuditUserReadModel?> GetUserAsync(int userId, CancellationToken cancellationToken = default) => Task.FromResult(users.SingleOrDefault(user => user.UserId == userId));

        public Task<IReadOnlyList<AuditMatchBetReadModel>> ListMatchBetsAsync(int? userId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AuditMatchBetReadModel>>(userId.HasValue ? matchBets.Where(item => item.UserId == userId.Value).ToArray() : matchBets);

        public Task<IReadOnlyList<AuditChallengePositionReadModel>> ListChallengePositionsAsync(int? userId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AuditChallengePositionReadModel>>(userId.HasValue ? challengePositions.Where(item => item.UserId == userId.Value).ToArray() : challengePositions);

        public Task<IReadOnlyList<AuditTournamentPickReadModel>> ListTournamentPicksAsync(int? userId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AuditTournamentPickReadModel>>(userId.HasValue ? tournamentPicks.Where(item => item.UserId == userId.Value).ToArray() : tournamentPicks);

        public Task<AuditTournamentSettlementReadModel?> GetTournamentSettlementAsync(CancellationToken cancellationToken = default) => Task.FromResult(settlement);
    }
}
