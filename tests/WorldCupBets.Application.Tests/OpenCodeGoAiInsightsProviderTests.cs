using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using WorldCupBets.Application.Abstractions;
using WorldCupBets.Infrastructure.AiInsights;
using Xunit;

namespace WorldCupBets.Application.Tests;

public sealed class OpenCodeGoAiInsightsProviderTests
{
    private static readonly MatchInsightsPrompt SamplePrompt = new(
        "Argentina",
        "France",
        "Final",
        "Knockout",
        new DateTime(2026, 7, 19, 18, 0, 0, DateTimeKind.Utc),
        "MetLife Stadium",
        [new GroupStandingRow("Argentina", 3, 2, 1, 0, 5, 1, 4, 7)],
        [new GroupStandingRow("France", 3, 2, 0, 1, 6, 3, 3, 6)]);

    [Fact]
    public async Task GenerateAsync_Maps_Structured_Content_From_Chat_Completion()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {
                        "choices": [
                            {
                                "message": {
                                    "content": "{ \"facts\": [{ \"text\": \"Argentina won the 2022 World Cup.\" }], \"antecedents\": [{ \"text\": \"These two teams met in the 2022 final.\" }], \"qa\": [{ \"question\": \"Who won in 2022?\", \"answer\": \"Argentina, on penalties.\" }] }"
                                }
                            }
                        ]
                    }
                    """),
            });
        var provider = new OpenCodeGoAiInsightsProvider(
            new HttpClient(handler) { BaseAddress = new Uri("https://example.test") },
            new AiInsightsOptions { ApiKey = "test-key" },
            NullLogger<OpenCodeGoAiInsightsProvider>.Instance);

        var result = await provider.GenerateAsync(SamplePrompt, CancellationToken.None);

        Assert.True(result.IsAvailable);
        Assert.Single(result.Facts);
        Assert.Equal("Argentina won the 2022 World Cup.", result.Facts[0].Text);
        Assert.Single(result.Antecedents);
        Assert.Equal("These two teams met in the 2022 final.", result.Antecedents[0].Text);
        Assert.Single(result.Qa);
        Assert.Equal("Who won in 2022?", result.Qa[0].Question);
        Assert.Equal("Argentina, on penalties.", result.Qa[0].Answer);
    }

    [Fact]
    public async Task GenerateAsync_Returns_Unavailable_When_Provider_Returns_Non_Success_Status()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var provider = new OpenCodeGoAiInsightsProvider(
            new HttpClient(handler) { BaseAddress = new Uri("https://example.test") },
            new AiInsightsOptions { ApiKey = "test-key" },
            NullLogger<OpenCodeGoAiInsightsProvider>.Instance);

        var result = await provider.GenerateAsync(SamplePrompt, CancellationToken.None);

        Assert.False(result.IsAvailable);
        Assert.Empty(result.Facts);
        Assert.Empty(result.Antecedents);
        Assert.Empty(result.Qa);
    }

    [Fact]
    public async Task GenerateAsync_Returns_Unavailable_When_Content_Is_Not_Valid_Json()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {
                        "choices": [
                            { "message": { "content": "Sorry, I cannot help with that." } }
                        ]
                    }
                    """),
            });
        var provider = new OpenCodeGoAiInsightsProvider(
            new HttpClient(handler) { BaseAddress = new Uri("https://example.test") },
            new AiInsightsOptions { ApiKey = "test-key" },
            NullLogger<OpenCodeGoAiInsightsProvider>.Instance);

        var result = await provider.GenerateAsync(SamplePrompt, CancellationToken.None);

        Assert.False(result.IsAvailable);
    }

    [Fact]
    public async Task GenerateAsync_Returns_Unavailable_When_Request_Throws()
    {
        var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("boom"));
        var provider = new OpenCodeGoAiInsightsProvider(
            new HttpClient(handler) { BaseAddress = new Uri("https://example.test") },
            new AiInsightsOptions { ApiKey = "test-key" },
            NullLogger<OpenCodeGoAiInsightsProvider>.Instance);

        var result = await provider.GenerateAsync(SamplePrompt, CancellationToken.None);

        Assert.False(result.IsAvailable);
    }

    [Fact]
    public async Task GenerateAsync_Returns_Unavailable_Without_Calling_Provider_When_ApiKey_Is_Blank()
    {
        var called = false;
        var handler = new StubHttpMessageHandler(_ =>
        {
            called = true;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var provider = new OpenCodeGoAiInsightsProvider(
            new HttpClient(handler) { BaseAddress = new Uri("https://example.test") },
            new AiInsightsOptions { ApiKey = "" },
            NullLogger<OpenCodeGoAiInsightsProvider>.Instance);

        var result = await provider.GenerateAsync(SamplePrompt, CancellationToken.None);

        Assert.False(result.IsAvailable);
        Assert.False(called);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(responseFactory(request));
        }
    }
}
