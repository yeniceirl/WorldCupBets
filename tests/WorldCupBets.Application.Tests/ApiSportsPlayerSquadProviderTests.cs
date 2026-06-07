using System.Net;
using WorldCupBets.Application.Abstractions;
using WorldCupBets.Infrastructure.ExternalFootball;
using Xunit;

namespace WorldCupBets.Application.Tests;

public sealed class ApiSportsPlayerSquadProviderTests
{
    [Fact]
    public async Task ResolveTeamIdAsync_Returns_National_Team_Id_For_Best_Match()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {
                        "response": [
                            { "team": { "id": 26, "name": "Argentina", "national": true } },
                            { "team": { "id": 99, "name": "Argentina U23", "national": false } }
                        ]
                    }
                    """),
            });
        var provider = new ApiSportsPlayerSquadProvider(
            new HttpClient(handler) { BaseAddress = new Uri("https://example.test") },
            new ApiSportsFootballOptions { ApiKey = "test-key" });

        var teamId = await provider.ResolveTeamIdAsync("Argentina", CancellationToken.None);

        Assert.Equal("26", teamId);
    }

    [Fact]
    public async Task ResolveTeamIdAsync_Returns_Null_When_No_National_Team_Found()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{ \"response\": [] }"),
            });
        var provider = new ApiSportsPlayerSquadProvider(
            new HttpClient(handler) { BaseAddress = new Uri("https://example.test") },
            new ApiSportsFootballOptions { ApiKey = "test-key" });

        var teamId = await provider.ResolveTeamIdAsync("Atlantis", CancellationToken.None);

        Assert.Null(teamId);
    }

    [Fact]
    public async Task GetSquadAsync_Maps_Players_From_Response()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {
                        "response": [
                            {
                                "team": { "name": "Argentina" },
                                "players": [
                                    { "id": 154, "name": "L. Messi", "position": "Attacker", "photo": "https://example.test/messi.png" },
                                    { "id": null, "name": "Unknown" }
                                ]
                            }
                        ]
                    }
                    """),
            });
        var provider = new ApiSportsPlayerSquadProvider(
            new HttpClient(handler) { BaseAddress = new Uri("https://example.test") },
            new ApiSportsFootballOptions { ApiKey = "test-key" });

        var squad = await provider.GetSquadAsync("26", CancellationToken.None);

        Assert.Single(squad);
        Assert.Equal("api-sports:154", squad[0].ExternalId);
        Assert.Equal("L. Messi", squad[0].Name);
        Assert.Equal("Argentina", squad[0].TeamName);
        Assert.Equal("Attacker", squad[0].Position);
    }

    [Fact]
    public async Task GetSquadAsync_Throws_RateLimitException_On_Http429()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests));
        var provider = new ApiSportsPlayerSquadProvider(
            new HttpClient(handler) { BaseAddress = new Uri("https://example.test") },
            new ApiSportsFootballOptions { ApiKey = "test-key" });

        await Assert.ThrowsAsync<ApiSportsRateLimitException>(() => provider.GetSquadAsync("26", CancellationToken.None));
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(responseFactory(request));
        }
    }
}
