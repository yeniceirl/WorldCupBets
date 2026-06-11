using System.Reflection;
using WorldCupBets.Application.Abstractions;
using WorldCupBets.Application.Features.Matches;
using WorldCupBets.Domain.Common;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;
using Xunit;

namespace WorldCupBets.Application.Tests;

public sealed class GetMatchInsightsHandlerTests
{
    [Fact]
    public async Task Handle_Returns_Available_Dto_When_Provider_Succeeds()
    {
        var match = Match.Create(MatchPhase.GroupStage, "Argentina", "Japan", DateTime.UtcNow.AddHours(1), "MetLife Stadium");
        SetEntityId(match, 20);

        var providerResult = new MatchInsightsResult(
            true,
            [new InsightFact("Argentina won the 2022 World Cup.")],
            [new InsightAntecedent("These two teams have met twice in World Cup history.")],
            [new InsightQaPair("Who has the most World Cup titles?", "Brazil, with five titles.")]);
        var provider = new StubAiInsightsProvider(providerResult);

        var dto = await GetMatchInsightsHandler.Handle(
            new GetMatchInsightsQuery(match.Id),
            new StubMatchRepository(match),
            new StubExternalFootballDataRepository(EmptySnapshot()),
            provider,
            CancellationToken.None);

        Assert.True(dto.IsAvailable);
        Assert.Single(dto.Facts);
        Assert.Equal("Argentina won the 2022 World Cup.", dto.Facts[0].Text);
        Assert.Single(dto.Antecedents);
        Assert.Equal("These two teams have met twice in World Cup history.", dto.Antecedents[0].Text);
        Assert.Single(dto.Qa);
        Assert.Equal("Who has the most World Cup titles?", dto.Qa[0].Question);
        Assert.Equal("Brazil, with five titles.", dto.Qa[0].Answer);
    }

    [Fact]
    public async Task Handle_Returns_Unavailable_Dto_When_Provider_Is_Unavailable()
    {
        var match = Match.Create(MatchPhase.GroupStage, "Argentina", "Japan", DateTime.UtcNow.AddHours(1), "MetLife Stadium");
        SetEntityId(match, 20);

        var provider = new StubAiInsightsProvider(MatchInsightsResult.Unavailable);

        var dto = await GetMatchInsightsHandler.Handle(
            new GetMatchInsightsQuery(match.Id),
            new StubMatchRepository(match),
            new StubExternalFootballDataRepository(EmptySnapshot()),
            provider,
            CancellationToken.None);

        Assert.False(dto.IsAvailable);
        Assert.Empty(dto.Facts);
        Assert.Empty(dto.Antecedents);
        Assert.Empty(dto.Qa);
    }

    [Fact]
    public async Task Handle_Returns_Unavailable_Dto_When_Match_Is_Not_Found()
    {
        var provider = new StubAiInsightsProvider(MatchInsightsResult.Unavailable);

        var dto = await GetMatchInsightsHandler.Handle(
            new GetMatchInsightsQuery(999),
            new StubMatchRepository(),
            new StubExternalFootballDataRepository(EmptySnapshot()),
            provider,
            CancellationToken.None);

        Assert.False(dto.IsAvailable);
        Assert.Empty(dto.Facts);
        Assert.Empty(dto.Antecedents);
        Assert.Empty(dto.Qa);
        Assert.Null(provider.ReceivedPrompt);
    }

    [Fact]
    public async Task Handle_Builds_Prompt_With_Team_Names_Stage_Group_And_Standings()
    {
        var match = Match.Create(MatchPhase.GroupStage, "Argentina", "Japan", DateTime.UtcNow.AddHours(1), "MetLife Stadium");
        SetEntityId(match, 20);
        SetProperty(match, nameof(Match.GroupName), "A");

        var snapshot = new ExternalFootballSnapshot(
            [
                new ExternalFootballTeamDto("t-arg", "Argentina", "ARG", "AR", "A", null),
                new ExternalFootballTeamDto("t-jpn", "Japan", "JPN", "JP", "A", null)
            ],
            [],
            [],
            [
                new ExternalFootballMatchDto("1", "t-arg", "t-jpn", "Argentina", "Japan", null, null, "A", "1", "06/11/2026 13:00", "stadium-1", true, "finished", "group", 2, 0)
            ],
            DateTime.UtcNow);

        var provider = new StubAiInsightsProvider(MatchInsightsResult.Unavailable);

        await GetMatchInsightsHandler.Handle(
            new GetMatchInsightsQuery(match.Id),
            new StubMatchRepository(match),
            new StubExternalFootballDataRepository(snapshot),
            provider,
            CancellationToken.None);

        var prompt = provider.ReceivedPrompt;
        Assert.NotNull(prompt);
        Assert.Equal("Argentina", prompt!.HomeTeamName);
        Assert.Equal("Japan", prompt.AwayTeamName);
        Assert.Equal("Group Stage", prompt.Stage);
        Assert.Equal("A", prompt.GroupName);

        Assert.Equal(2, prompt.HomeTeamGroupStandings.Count);
        Assert.Contains(prompt.HomeTeamGroupStandings, row => row.TeamName == "Argentina" && row.Points == 3);
        Assert.Contains(prompt.HomeTeamGroupStandings, row => row.TeamName == "Japan" && row.Points == 0);

        Assert.Equal(2, prompt.AwayTeamGroupStandings.Count);
        Assert.Contains(prompt.AwayTeamGroupStandings, row => row.TeamName == "Argentina" && row.Points == 3);
        Assert.Contains(prompt.AwayTeamGroupStandings, row => row.TeamName == "Japan" && row.Points == 0);
    }

    private static ExternalFootballSnapshot EmptySnapshot()
    {
        return new ExternalFootballSnapshot([], [], [], [], DateTime.UtcNow);
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

    private sealed class StubAiInsightsProvider(MatchInsightsResult result) : IAiInsightsProvider
    {
        public MatchInsightsPrompt? ReceivedPrompt { get; private set; }

        public Task<MatchInsightsResult> GenerateAsync(MatchInsightsPrompt prompt, CancellationToken cancellationToken = default)
        {
            ReceivedPrompt = prompt;
            return Task.FromResult(result);
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
}
