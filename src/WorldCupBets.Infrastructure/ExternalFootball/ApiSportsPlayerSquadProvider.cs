using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using WorldCupBets.Application.Abstractions;

namespace WorldCupBets.Infrastructure.ExternalFootball;

public sealed class ApiSportsPlayerSquadProvider(HttpClient httpClient, ApiSportsFootballOptions options) : IPlayerSquadProvider
{
    public const string Provider = "api-sports";

    // API-Sports' free plan throttles to roughly 10 requests/minute. Pacing requests at this
    // interval keeps a sync run under that ceiling so it completes in one pass instead of
    // tripping the per-minute limit (and the abort-on-429 safeguard) partway through.
    private static readonly TimeSpan MinRequestInterval = TimeSpan.FromSeconds(7);

    private DateTime? lastRequestAtUtc;

    public string ProviderName => Provider;

    public async Task<string?> ResolveTeamIdAsync(string teamName, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<ApiSportsTeamsResponse>($"/teams?search={Uri.EscapeDataString(teamName)}", cancellationToken);
        var normalizedTeamName = Normalize(teamName);

        var team = (response?.Response ?? [])
            .Select(candidate => candidate.Team)
            .Where(team => team is not null && team.National && !string.IsNullOrWhiteSpace(team.Name))
            .OrderByDescending(team => Normalize(team!.Name!).Equals(normalizedTeamName, StringComparison.OrdinalIgnoreCase))
            .ThenBy(team => team!.Name)
            .FirstOrDefault();

        return team is null ? null : team.Id.ToString(CultureInfo.InvariantCulture);
    }

    public async Task<IReadOnlyList<PlayerSquadMemberDto>> GetSquadAsync(string teamExternalId, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<ApiSportsSquadResponse>($"/players/squads?team={teamExternalId}", cancellationToken);
        var squad = response?.Response?.FirstOrDefault();
        if (squad is null)
        {
            return [];
        }

        return (squad.Players ?? [])
            .Where(player => player.Id is not null && !string.IsNullOrWhiteSpace(player.Name))
            .Select(player => new PlayerSquadMemberDto(
                $"api-sports:{player.Id}",
                player.Name!,
                squad.Team?.Name,
                player.Position,
                player.PhotoUrl))
            .ToArray();
    }

    private async Task<T?> SendAsync<T>(string path, CancellationToken cancellationToken)
    {
        await PaceRequestAsync(cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("x-apisports-key", options.ApiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new ApiSportsRateLimitException("API-Sports rate limit (HTTP 429) reached.");
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }

    private async Task PaceRequestAsync(CancellationToken cancellationToken)
    {
        if (lastRequestAtUtc is { } lastRequestAt)
        {
            var elapsed = DateTime.UtcNow - lastRequestAt;
            if (elapsed < MinRequestInterval)
            {
                await Task.Delay(MinRequestInterval - elapsed, cancellationToken);
            }
        }

        lastRequestAtUtc = DateTime.UtcNow;
    }

    private static string Normalize(string value)
    {
        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private sealed record ApiSportsTeamsResponse([property: JsonPropertyName("response")] IReadOnlyList<ApiSportsTeamItem>? Response);

    private sealed record ApiSportsTeamItem([property: JsonPropertyName("team")] ApiSportsTeam? Team);

    private sealed record ApiSportsTeam(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("national")] bool National);

    private sealed record ApiSportsSquadResponse([property: JsonPropertyName("response")] IReadOnlyList<ApiSportsSquad>? Response);

    private sealed record ApiSportsSquad(
        [property: JsonPropertyName("team")] ApiSportsSquadTeam? Team,
        [property: JsonPropertyName("players")] IReadOnlyList<ApiSportsSquadPlayer>? Players);

    private sealed record ApiSportsSquadTeam([property: JsonPropertyName("name")] string? Name);

    private sealed record ApiSportsSquadPlayer(
        [property: JsonPropertyName("id")] int? Id,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("position")] string? Position,
        [property: JsonPropertyName("photo")] string? PhotoUrl);
}
