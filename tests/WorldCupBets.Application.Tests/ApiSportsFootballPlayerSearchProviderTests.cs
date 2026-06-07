using WorldCupBets.Application.Abstractions;
using WorldCupBets.Infrastructure.ExternalFootball;
using Xunit;

namespace WorldCupBets.Application.Tests;

public sealed class ApiSportsFootballPlayerSearchProviderTests
{
    [Fact]
    public async Task SearchAsync_Returns_Persisted_Matches_Ranked_By_Starts_With_Word_Then_Alphabetical()
    {
        var repository = new StubExternalFootballPlayerRepository(
        [
            new ExternalFootballPlayerDto("api-sports:217", "Lautaro Martinez", "lautaro martinez", "26", "Argentina", "Attacker", "https://example.test/lautaro.png"),
            new ExternalFootballPlayerDto("api-sports:154", "L. Messi", "l. messi", "26", "Argentina", "Attacker", "https://example.test/messi.png"),
            new ExternalFootballPlayerDto("api-sports:300", "Marco Messias", "marco messias", "47", "Brazil", "Defender", null),
        ]);
        var provider = new ApiSportsFootballPlayerSearchProvider(repository);

        var results = await provider.SearchAsync("mes", CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Equal("api-sports:154", results[0].ExternalId);
        Assert.Equal("L. Messi", results[0].Name);
        Assert.Equal("Argentina", results[0].TeamName);
        Assert.Equal("api-sports:300", results[1].ExternalId);
        Assert.Equal("Marco Messias", results[1].Name);
        Assert.Equal("api-sports", repository.LastProviderName);
        Assert.Equal("mes", repository.LastNormalizedQuery);
    }

    [Fact]
    public async Task SearchAsync_Returns_Empty_When_Query_Is_Too_Short_And_Does_Not_Query_Repository()
    {
        var repository = new StubExternalFootballPlayerRepository(
        [
            new ExternalFootballPlayerDto("api-sports:154", "L. Messi", "l. messi", "26", "Argentina", "Attacker", null),
        ]);
        var provider = new ApiSportsFootballPlayerSearchProvider(repository);

        var results = await provider.SearchAsync("me", CancellationToken.None);

        Assert.Empty(results);
        Assert.False(repository.WasQueried);
    }

    [Fact]
    public async Task SearchAsync_Returns_Empty_When_No_Players_Persisted()
    {
        var repository = new StubExternalFootballPlayerRepository([]);
        var provider = new ApiSportsFootballPlayerSearchProvider(repository);

        var results = await provider.SearchAsync("messi", CancellationToken.None);

        Assert.Empty(results);
        Assert.True(repository.WasQueried);
    }

    [Fact]
    public async Task SearchAsync_Limits_Results_To_Ten()
    {
        var players = Enumerable.Range(1, 15)
            .Select(index => new ExternalFootballPlayerDto(
                $"api-sports:{index}",
                $"Player Messi {index:00}",
                $"player messi {index:00}",
                "26",
                "Argentina",
                "Attacker",
                null))
            .ToArray();
        var repository = new StubExternalFootballPlayerRepository(players);
        var provider = new ApiSportsFootballPlayerSearchProvider(repository);

        var results = await provider.SearchAsync("messi", CancellationToken.None);

        Assert.Equal(10, results.Count);
    }

    private sealed class StubExternalFootballPlayerRepository(IReadOnlyList<ExternalFootballPlayerDto> players) : IExternalFootballPlayerRepository
    {
        public bool WasQueried { get; private set; }

        public string? LastProviderName { get; private set; }

        public string? LastNormalizedQuery { get; private set; }

        public Task ReplacePlayersAsync(string providerName, IReadOnlyList<ExternalFootballPlayerDto> players, DateTime syncedAtUtc, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<ExternalFootballPlayerDto>> SearchAsync(string providerName, string normalizedQuery, CancellationToken cancellationToken = default)
        {
            WasQueried = true;
            LastProviderName = providerName;
            LastNormalizedQuery = normalizedQuery;

            var matches = players
                .Where(player => player.NormalizedName.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            return Task.FromResult<IReadOnlyList<ExternalFootballPlayerDto>>(matches);
        }

        public Task<IReadOnlyDictionary<string, string>> GetTeamIdMapAsync(string providerName, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyDictionary<string, string?>> GetPhotoUrlsByExternalIdsAsync(string providerName, IReadOnlyCollection<string> externalIds, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
