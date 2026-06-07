using System.Net;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using WorldCupBets.Application.Abstractions;
using WorldCupBets.Infrastructure.ExternalFootball;
using Xunit;

namespace WorldCupBets.Application.Tests;

public sealed class ApiSportsFootballPlayerSearchProviderTests
{
    [Fact]
    public async Task SearchAsync_Builds_Squad_Index_And_Filters_Three_Character_Query()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            var pathAndQuery = request.RequestUri!.PathAndQuery;
            if (pathAndQuery.Equals("/teams?search=Argentina", StringComparison.OrdinalIgnoreCase))
            {
                return """
                    {
                        "response": [
                            { "team": { "id": 26, "name": "Argentina", "national": true } }
                        ]
                    }
                    """;
            }

            if (pathAndQuery.Equals("/players/squads?team=26", StringComparison.OrdinalIgnoreCase))
            {
                return """
                    {
                        "response": [
                            {
                                "team": { "id": 26, "name": "Argentina" },
                                "players": [
                                    { "id": 154, "name": "L. Messi", "position": "Attacker", "photo": "https://example.test/messi.png" },
                                    { "id": 217, "name": "Lautaro Martinez", "position": "Attacker", "photo": "https://example.test/lautaro.png" }
                                ]
                            }
                        ]
                    }
                    """;
            }

            return "{ \"response\": [] }";
        });
        var provider = new ApiSportsFootballPlayerSearchProvider(
            new HttpClient(handler) { BaseAddress = new Uri("https://example.test") },
            new ApiSportsFootballOptions { ApiKey = "test-key" },
            new ExternalFootballDataOptions { Provider = "worldcup26", BaseUrl = "https://worldcup26.ir" },
            new StubExternalFootballDataRepository(new ExternalFootballSnapshot(
                [new ExternalFootballTeamDto("37", "Argentina", "ARG", "AR", "J", null)],
                [],
                [],
                [],
                DateTime.UtcNow)),
            CreateHybridCache());

        var results = await provider.SearchAsync("mes", CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("api-sports:154", results[0].ExternalId);
        Assert.Equal("L. Messi", results[0].Name);
        Assert.Equal("Argentina", results[0].TeamName);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task SearchAsync_Returns_Empty_When_Query_Is_Too_Short()
    {
        var handler = new StubHttpMessageHandler(_ => "{ \"response\": [] }");
        var provider = new ApiSportsFootballPlayerSearchProvider(
            new HttpClient(handler) { BaseAddress = new Uri("https://example.test") },
            new ApiSportsFootballOptions { ApiKey = "test-key" },
            new ExternalFootballDataOptions { Provider = "worldcup26", BaseUrl = "https://worldcup26.ir" },
            new StubExternalFootballDataRepository(null),
            CreateHybridCache());

        var results = await provider.SearchAsync("me", CancellationToken.None);

        Assert.Empty(results);
        Assert.Empty(handler.Requests);
    }

    private static HybridCache CreateHybridCache()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();

        return services
            .BuildServiceProvider()
            .GetRequiredService<HybridCache>();
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

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, string> responseFactory) : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseFactory(request)),
            });
        }
    }
}
