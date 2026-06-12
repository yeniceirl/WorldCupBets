using System.Net;
using WorldCupBets.Application.Abstractions;
using WorldCupBets.Infrastructure.ExternalFootball;
using Xunit;

namespace WorldCupBets.Application.Tests;

public sealed class ApiSportsOfficialMatchResultProviderTests
{
    [Fact]
    public async Task TryConfirmAsync_Returns_Final_Score_For_Matching_Fixture()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "response": [
                        {
                          "fixture": {
                            "id": 9001,
                            "date": "2026-06-11T18:00:00+00:00",
                            "status": { "short": "FT" }
                          },
                          "teams": {
                            "home": { "name": "Mexico" },
                            "away": { "name": "South Africa" }
                          },
                          "goals": {
                            "home": 2,
                            "away": 1
                          }
                        }
                      ]
                    }
                    """)
            });

        var provider = new ApiSportsOfficialMatchResultProvider(
            new HttpClient(handler) { BaseAddress = new Uri("https://example.test") },
            new ApiSportsFootballOptions { ApiKey = "test-key" });

        var result = await provider.TryConfirmAsync(
            new OfficialMatchResultLookup("Mexico", "South Africa", new DateTime(2026, 6, 11, 18, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 6, 11)),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("9001", result!.SourceReference);
        Assert.Equal(2, result.HomeScore);
        Assert.Equal(1, result.AwayScore);
    }

    [Fact]
    public async Task TryConfirmAsync_Returns_Null_When_Fixture_Is_Not_Final()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "response": [
                        {
                          "fixture": {
                            "id": 9001,
                            "date": "2026-06-11T18:00:00+00:00",
                            "status": { "short": "2H" }
                          },
                          "teams": {
                            "home": { "name": "Mexico" },
                            "away": { "name": "South Africa" }
                          },
                          "goals": {
                            "home": 1,
                            "away": 1
                          }
                        }
                      ]
                    }
                    """)
            });

        var provider = new ApiSportsOfficialMatchResultProvider(
            new HttpClient(handler) { BaseAddress = new Uri("https://example.test") },
            new ApiSportsFootballOptions { ApiKey = "test-key" });

        var result = await provider.TryConfirmAsync(
            new OfficialMatchResultLookup("Mexico", "South Africa", new DateTime(2026, 6, 11, 18, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 6, 11)),
            CancellationToken.None);

        Assert.Null(result);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(responseFactory(request));
        }
    }
}
