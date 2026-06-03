using WorldCupBets.Application.Features.Matches;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;
using Xunit;

namespace WorldCupBets.Application.Tests;

public sealed class MatchRulesTests
{
    [Theory]
    [InlineData(MatchPhase.GroupStage, 5, "Group Stage")]
    [InlineData(MatchPhase.RoundOf32, 10, "Round of 32")]
    [InlineData(MatchPhase.RoundOf16, 15, "Round of 16")]
    [InlineData(MatchPhase.Quarterfinals, 20, "Quarterfinals")]
    [InlineData(MatchPhase.Semifinals, 30, "Semifinals")]
    [InlineData(MatchPhase.ThirdPlace, 20, "Third Place")]
    [InlineData(MatchPhase.Final, 40, "Final")]
    public void MatchPhase_Maps_To_Reglamento_Stake_And_Label(MatchPhase phase, int expectedStake, string expectedLabel)
    {
        var match = Match.Create(phase, "Home", "Away", DateTime.UtcNow.AddHours(1), "Venue");

        Assert.Equal(expectedStake, match.GetStakeAmountCc());
        Assert.Equal(expectedLabel, match.GetStageLabel());
    }

    [Fact]
    public void IsBettingOpenAt_Closes_Five_Minutes_After_Kickoff()
    {
        var startsAtUtc = new DateTime(2026, 6, 14, 18, 0, 0, DateTimeKind.Utc);
        var match = Match.Create(MatchPhase.GroupStage, "Argentina", "Japan", startsAtUtc, "MetLife Stadium");

        Assert.True(match.IsBettingOpenAt(startsAtUtc.AddMinutes(5)));
        Assert.False(match.IsBettingOpenAt(startsAtUtc.AddMinutes(5).AddSeconds(1)));
        Assert.Equal(startsAtUtc.AddMinutes(5), match.GetBettingClosesAtUtc());
    }

    [Fact]
    public void RecordOfficialResult_Requires_Closed_Betting_Window()
    {
        var startsAtUtc = new DateTime(2026, 6, 14, 18, 0, 0, DateTimeKind.Utc);
        var match = Match.Create(MatchPhase.GroupStage, "Argentina", "Japan", startsAtUtc, "MetLife Stadium");

        Assert.Throws<InvalidOperationException>(() =>
            match.RecordOfficialResult(MatchBetSelection.Home, startsAtUtc.AddMinutes(5)));

        match.RecordOfficialResult(MatchBetSelection.Home, startsAtUtc.AddMinutes(5).AddSeconds(1));

        Assert.Equal(MatchBetSelection.Home, match.OfficialResult);
    }

    [Fact]
    public void CreditBalance_Increases_Current_Balance()
    {
        var user = User.Create("google-sub", "user@example.com", "User");

        user.CreditBalance(25);

        Assert.Equal(User.InitialBalanceCc + 25, user.CurrentBalanceCc);
    }

    [Fact]
    public async Task Handle_Exposes_Regulation_Metadata_And_Current_User_Bet_For_Matches()
    {
        var match = Match.Create(MatchPhase.GroupStage, "Argentina", "Japan", DateTime.UtcNow.AddHours(1), "MetLife Stadium");
        SetEntityId(match, 7);
        var repository = new StubMatchRepository(match);
        var betsRepository = new StubMatchBetRepository(MatchBet.Create(99, 7, MatchBetSelection.Draw, 5, DateTime.UtcNow));

        var result = await GetMatchesHandler.Handle(new GetMatchesQuery(99), repository, betsRepository, CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal(5, item.StakeAmountCc);
        Assert.True(item.IsBettingOpen);
        Assert.Equal(match.GetBettingClosesAtUtc(), item.BettingClosesAtUtc);
        Assert.Equal("Draw", item.CurrentUserBetSelection);
        Assert.Equal("Group Stage", item.Stage);
    }

    private static void SetEntityId(WorldCupBets.Domain.Common.Entity entity, int id)
    {
        var property = typeof(WorldCupBets.Domain.Common.Entity).GetProperty(nameof(WorldCupBets.Domain.Common.Entity.Id));
        property!.SetValue(entity, id);
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

        public Task AddAsync(MatchBet matchBet, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
