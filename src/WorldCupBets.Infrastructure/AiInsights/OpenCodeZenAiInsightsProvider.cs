using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using WorldCupBets.Application.Abstractions;

namespace WorldCupBets.Infrastructure.AiInsights;

public sealed class OpenCodeZenAiInsightsProvider(HttpClient httpClient, AiInsightsOptions options) : IAiInsightsProvider
{
    private const string SystemPrompt =
        "You are a football (soccer) trivia assistant for the FIFA World Cup 2026. " +
        "Given a match's context (teams, stage, group, and group standings), produce concise, " +
        "factual insight content as STRICT JSON matching this schema: " +
        "{ \"facts\": [{ \"text\": string }], \"antecedents\": [{ \"text\": string }], \"qa\": [{ \"question\": string, \"answer\": string }] }. " +
        "Provide 2-3 short \"did you know\" facts grounded in the provided tournament context, " +
        "2-3 head-to-head antecedents (notable historical meetings, streaks, or results between these two national teams), " +
        "and 1-2 short question-and-answer pairs. " +
        "Respond with ONLY the JSON object, no markdown, no commentary, no extra keys.";

    private static readonly JsonSerializerOptions ResponseJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<MatchInsightsResult> GenerateAsync(MatchInsightsPrompt prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return MatchInsightsResult.Unavailable;
        }

        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(options.TimeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            using var request = new HttpRequestMessage(HttpMethod.Post, "/chat/completions");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ApiKey);
            request.Content = JsonContent.Create(BuildRequestBody(prompt));

            using var response = await httpClient.SendAsync(request, linkedCts.Token);
            if (!response.IsSuccessStatusCode)
            {
                return MatchInsightsResult.Unavailable;
            }

            var payload = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(linkedCts.Token);
            var content = payload?.Choices?.FirstOrDefault()?.Message?.Content;
            if (string.IsNullOrWhiteSpace(content))
            {
                return MatchInsightsResult.Unavailable;
            }

            return ParseInsightContent(content);
        }
        catch
        {
            return MatchInsightsResult.Unavailable;
        }
    }

    private ChatCompletionRequest BuildRequestBody(MatchInsightsPrompt prompt)
    {
        return new ChatCompletionRequest(
            options.Model,
            [
                new ChatMessage("system", SystemPrompt),
                new ChatMessage("user", BuildUserMessage(prompt))
            ],
            options.MaxTokens,
            0.6);
    }

    private static string BuildUserMessage(MatchInsightsPrompt prompt)
    {
        var payload = new
        {
            homeTeam = prompt.HomeTeamName,
            awayTeam = prompt.AwayTeamName,
            stage = prompt.Stage,
            group = prompt.GroupName,
            startsAtUtc = prompt.StartsAtUtc,
            venue = prompt.Venue,
            homeTeamGroupStandings = prompt.HomeTeamGroupStandings,
            awayTeamGroupStandings = prompt.AwayTeamGroupStandings
        };

        return JsonSerializer.Serialize(payload, ResponseJsonOptions);
    }

    private static MatchInsightsResult ParseInsightContent(string content)
    {
        var json = ExtractJsonObject(content);
        if (json is null)
        {
            return MatchInsightsResult.Unavailable;
        }

        var parsed = JsonSerializer.Deserialize<InsightContentPayload>(json, ResponseJsonOptions);
        if (parsed is null)
        {
            return MatchInsightsResult.Unavailable;
        }

        var facts = (parsed.Facts ?? [])
            .Where(fact => !string.IsNullOrWhiteSpace(fact.Text))
            .Select(fact => new InsightFact(fact.Text!.Trim()))
            .ToArray();
        var antecedents = (parsed.Antecedents ?? [])
            .Where(antecedent => !string.IsNullOrWhiteSpace(antecedent.Text))
            .Select(antecedent => new InsightAntecedent(antecedent.Text!.Trim()))
            .ToArray();
        var qa = (parsed.Qa ?? [])
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Question) && !string.IsNullOrWhiteSpace(pair.Answer))
            .Select(pair => new InsightQaPair(pair.Question!.Trim(), pair.Answer!.Trim()))
            .ToArray();

        if (facts.Length == 0 && antecedents.Length == 0 && qa.Length == 0)
        {
            return MatchInsightsResult.Unavailable;
        }

        return new MatchInsightsResult(true, facts, antecedents, qa);
    }

    private static string? ExtractJsonObject(string content)
    {
        var trimmed = content.Trim();
        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return null;
        }

        return trimmed[start..(end + 1)];
    }

    private sealed record ChatCompletionRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<ChatMessage> Messages,
        [property: JsonPropertyName("max_tokens")] int MaxTokens,
        [property: JsonPropertyName("temperature")] double Temperature);

    private sealed record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ChatCompletionResponse([property: JsonPropertyName("choices")] IReadOnlyList<ChatCompletionChoice>? Choices);

    private sealed record ChatCompletionChoice([property: JsonPropertyName("message")] ChatCompletionMessage? Message);

    private sealed record ChatCompletionMessage([property: JsonPropertyName("content")] string? Content);

    private sealed record InsightContentPayload(
        [property: JsonPropertyName("facts")] IReadOnlyList<InsightFactPayload>? Facts,
        [property: JsonPropertyName("antecedents")] IReadOnlyList<InsightAntecedentPayload>? Antecedents,
        [property: JsonPropertyName("qa")] IReadOnlyList<InsightQaPayload>? Qa);

    private sealed record InsightFactPayload([property: JsonPropertyName("text")] string? Text);

    private sealed record InsightAntecedentPayload([property: JsonPropertyName("text")] string? Text);

    private sealed record InsightQaPayload(
        [property: JsonPropertyName("question")] string? Question,
        [property: JsonPropertyName("answer")] string? Answer);
}
