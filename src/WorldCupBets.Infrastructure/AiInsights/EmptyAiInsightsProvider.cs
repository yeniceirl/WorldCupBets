using WorldCupBets.Application.Abstractions;

namespace WorldCupBets.Infrastructure.AiInsights;

public sealed class EmptyAiInsightsProvider : IAiInsightsProvider
{
    public Task<MatchInsightsResult> GenerateAsync(MatchInsightsPrompt prompt, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(MatchInsightsResult.Unavailable);
    }
}
