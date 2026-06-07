using WorldCupBets.Application.Abstractions;
using WorldCupBets.Application.Features.FootballData;
using Xunit;

namespace WorldCupBets.Application.Tests;

public sealed class SyncPlayerSquadsHandlerTests
{
    private static readonly IReadOnlySet<string> TwoTeams = new HashSet<string>(["Argentina", "France"], StringComparer.OrdinalIgnoreCase);

    [Fact]
    public async Task Returns_NotConfigured_When_ApiKey_Is_Blank()
    {
        var provider = new FakeSquadProvider();
        var repository = new FakePlayerRepository();
        var options = new ApiSportsFootballSyncOptions(string.Empty, TwoTeams);

        var result = await SyncPlayerSquadsHandler.Handle(new SyncPlayerSquadsCommand(), provider, repository, options, CancellationToken.None);

        Assert.True(result.NotConfigured);
        Assert.Equal(0, result.TeamsProcessedCount);
        Assert.Equal(0, result.PlayersIndexedCount);
        Assert.Empty(result.Errors);
        Assert.False(provider.ResolveCalled);
        Assert.False(repository.ReplaceCalled);
    }

    [Fact]
    public async Task Returns_Zero_Result_When_No_Teams_Configured()
    {
        var provider = new FakeSquadProvider();
        var repository = new FakePlayerRepository();
        var options = new ApiSportsFootballSyncOptions("test-key", new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        var result = await SyncPlayerSquadsHandler.Handle(new SyncPlayerSquadsCommand(), provider, repository, options, CancellationToken.None);

        Assert.False(result.NotConfigured);
        Assert.Equal(0, result.TeamsProcessedCount);
        Assert.Equal(0, result.PlayersIndexedCount);
        Assert.Empty(result.Errors);
        Assert.False(provider.ResolveCalled);
        Assert.False(repository.ReplaceCalled);
    }

    [Fact]
    public async Task Aborts_Whole_Sync_On_First_RateLimit_And_Persists_Partial_Results()
    {
        var provider = new FakeSquadProvider
        {
            ResolveResults = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Argentina"] = "26",
                ["France"] = "2",
            },
            SquadResults = new Dictionary<string, IReadOnlyList<PlayerSquadMemberDto>>(StringComparer.OrdinalIgnoreCase)
            {
                ["26"] = [new PlayerSquadMemberDto("api-sports:1", "L. Messi", "Argentina", "Attacker", null)],
            },
            RateLimitedTeamIds = new HashSet<string>(["2"], StringComparer.OrdinalIgnoreCase),
        };
        var repository = new FakePlayerRepository();
        var options = new ApiSportsFootballSyncOptions("test-key", TwoTeams);

        var result = await SyncPlayerSquadsHandler.Handle(new SyncPlayerSquadsCommand(), provider, repository, options, CancellationToken.None);

        Assert.False(result.NotConfigured);
        Assert.Equal(1, result.TeamsProcessedCount);
        Assert.Equal(1, result.PlayersIndexedCount);
        Assert.Single(result.Errors);
        Assert.True(result.Errors[0].RateLimited);
        Assert.Equal("France", result.Errors[0].TeamName);
        Assert.True(repository.ReplaceCalled);
        Assert.Single(repository.ReplacedPlayers!);
        Assert.Equal("api-sports:1", repository.ReplacedPlayers![0].ExternalId);
    }

    [Fact]
    public async Task Collects_PerTeam_Errors_And_Continues_For_NonRateLimit_Failures()
    {
        var provider = new FakeSquadProvider
        {
            ResolveResults = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Argentina"] = null,
                ["France"] = "2",
            },
            SquadResults = new Dictionary<string, IReadOnlyList<PlayerSquadMemberDto>>(StringComparer.OrdinalIgnoreCase)
            {
                ["2"] = [new PlayerSquadMemberDto("api-sports:9", "K. Mbappe", "France", "Attacker", null)],
            },
        };
        var repository = new FakePlayerRepository();
        var options = new ApiSportsFootballSyncOptions("test-key", TwoTeams);

        var result = await SyncPlayerSquadsHandler.Handle(new SyncPlayerSquadsCommand(), provider, repository, options, CancellationToken.None);

        Assert.Equal(1, result.TeamsProcessedCount);
        Assert.Equal(1, result.PlayersIndexedCount);
        Assert.Single(result.Errors);
        Assert.False(result.Errors[0].RateLimited);
        Assert.Equal("Argentina", result.Errors[0].TeamName);
        Assert.True(repository.ReplaceCalled);
        Assert.Single(repository.ReplacedPlayers!);
        Assert.Equal("api-sports:9", repository.ReplacedPlayers![0].ExternalId);
    }

    [Fact]
    public async Task Reuses_Persisted_TeamExternalId_And_Skips_Resolution()
    {
        var provider = new FakeSquadProvider
        {
            ResolveResults = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["France"] = "2",
            },
            SquadResults = new Dictionary<string, IReadOnlyList<PlayerSquadMemberDto>>(StringComparer.OrdinalIgnoreCase)
            {
                ["26"] = [new PlayerSquadMemberDto("api-sports:1", "L. Messi", "Argentina", "Attacker", null)],
                ["2"] = [new PlayerSquadMemberDto("api-sports:9", "K. Mbappe", "France", "Attacker", null)],
            },
        };
        var repository = new FakePlayerRepository
        {
            TeamIdMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Argentina"] = "26" },
        };
        var options = new ApiSportsFootballSyncOptions("test-key", TwoTeams);

        var result = await SyncPlayerSquadsHandler.Handle(new SyncPlayerSquadsCommand(), provider, repository, options, CancellationToken.None);

        Assert.Equal(2, result.TeamsProcessedCount);
        Assert.Equal(2, result.PlayersIndexedCount);
        Assert.Empty(result.Errors);
        Assert.Single(provider.ResolvedTeamNames);
        Assert.Equal("France", provider.ResolvedTeamNames[0]);
    }

    private sealed class FakeSquadProvider : IPlayerSquadProvider
    {
        public string ProviderName => "api-sports";

        public IDictionary<string, string?> ResolveResults { get; init; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        public IDictionary<string, IReadOnlyList<PlayerSquadMemberDto>> SquadResults { get; init; } =
            new Dictionary<string, IReadOnlyList<PlayerSquadMemberDto>>(StringComparer.OrdinalIgnoreCase);

        public ISet<string> RateLimitedTeamIds { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public List<string> ResolvedTeamNames { get; } = [];

        public bool ResolveCalled { get; private set; }

        public Task<string?> ResolveTeamIdAsync(string teamName, CancellationToken cancellationToken = default)
        {
            ResolveCalled = true;
            ResolvedTeamNames.Add(teamName);
            ResolveResults.TryGetValue(teamName, out var teamId);
            return Task.FromResult(teamId);
        }

        public Task<IReadOnlyList<PlayerSquadMemberDto>> GetSquadAsync(string teamExternalId, CancellationToken cancellationToken = default)
        {
            if (RateLimitedTeamIds.Contains(teamExternalId))
            {
                throw new ApiSportsRateLimitException("API-Sports rate limit (HTTP 429) reached.");
            }

            return Task.FromResult(SquadResults.TryGetValue(teamExternalId, out var squad) ? squad : []);
        }
    }

    private sealed class FakePlayerRepository : IExternalFootballPlayerRepository
    {
        public IDictionary<string, string> TeamIdMap { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public bool ReplaceCalled { get; private set; }

        public IReadOnlyList<ExternalFootballPlayerDto>? ReplacedPlayers { get; private set; }

        public Task ReplacePlayersAsync(string providerName, IReadOnlyList<ExternalFootballPlayerDto> players, DateTime syncedAtUtc, CancellationToken cancellationToken = default)
        {
            ReplaceCalled = true;
            ReplacedPlayers = players;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ExternalFootballPlayerDto>> SearchAsync(string providerName, string normalizedQuery, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyDictionary<string, string>> GetTeamIdMapAsync(string providerName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>(TeamIdMap, StringComparer.OrdinalIgnoreCase));
        }
    }
}
