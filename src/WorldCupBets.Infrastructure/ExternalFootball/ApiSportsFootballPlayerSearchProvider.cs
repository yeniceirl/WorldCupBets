using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Hybrid;
using WorldCupBets.Application.Abstractions;

namespace WorldCupBets.Infrastructure.ExternalFootball;

public sealed class ApiSportsFootballPlayerSearchProvider(
    HttpClient httpClient,
    ApiSportsFootballOptions options,
    ExternalFootballDataOptions footballDataOptions,
    IExternalFootballDataRepository externalFootballDataRepository,
    HybridCache cache) : IPlayerSearchProvider
{
    private const string SquadIndexCacheKey = "api-sports:worldcup26:squad-index:v1";

    // TODO(player-squad-sync Phase 3): this provider is being rewritten to read persisted
    // ExternalFootballPlayer rows instead of building a live HybridCache index. This constant
    // is a temporary placeholder for the removed ApiSportsFootballOptions.SquadCacheHours setting.
    private static readonly TimeSpan SquadIndexCacheDuration = TimeSpan.FromHours(24);

    public async Task<IReadOnlyList<PlayerSearchResultDto>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var normalizedQuery = Normalize(query);
        if (normalizedQuery.Length < 3 || string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return [];
        }

        var players = await cache.GetOrCreateAsync(
            SquadIndexCacheKey,
            BuildPlayerIndexAsync,
            new HybridCacheEntryOptions
            {
                Expiration = SquadIndexCacheDuration,
                LocalCacheExpiration = TimeSpan.FromHours(2),
            },
            cancellationToken: cancellationToken);

        return players
            .Where(player => player.NormalizedName.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(player => StartsWithWord(player.NormalizedName, normalizedQuery))
            .ThenBy(player => player.Name)
            .Take(10)
            .Select(player => new PlayerSearchResultDto(
                player.ExternalId,
                player.Name,
                player.TeamName,
                player.TeamName,
                player.Position,
                player.PhotoUrl))
            .ToArray();
    }

    private async ValueTask<ApiSportsIndexedPlayer[]> BuildPlayerIndexAsync(CancellationToken cancellationToken)
    {
        var snapshot = await externalFootballDataRepository.GetSnapshotAsync(footballDataOptions.Provider, cancellationToken);
        if (snapshot is null)
        {
            return [];
        }

        var players = new List<ApiSportsIndexedPlayer>();
        foreach (var team in snapshot.Teams.Where(team => options.IncludedTeamNames.Contains(team.NameEn)))
        {
            var apiSportsTeam = await ResolveNationalTeamAsync(team.NameEn, cancellationToken);
            if (apiSportsTeam is null)
            {
                continue;
            }

            var response = await SendAsync<ApiSportsSquadResponse>($"/players/squads?team={apiSportsTeam.Id}", cancellationToken);
            var squad = response?.Response?.FirstOrDefault();
            if (squad is null)
            {
                continue;
            }

            players.AddRange((squad.Players ?? [])
                .Where(player => player.Id is not null && !string.IsNullOrWhiteSpace(player.Name))
                .Select(player => new ApiSportsIndexedPlayer(
                    $"api-sports:{player.Id}",
                    player.Name!,
                    Normalize(player.Name!),
                    squad.Team?.Name ?? team.NameEn,
                    player.Position,
                    player.PhotoUrl)));
        }

        return players
            .GroupBy(player => player.ExternalId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
    }

    private async Task<ApiSportsTeam?> ResolveNationalTeamAsync(string teamName, CancellationToken cancellationToken)
    {
        var response = await SendAsync<ApiSportsTeamsResponse>($"/teams?search={Uri.EscapeDataString(teamName)}", cancellationToken);
        var normalizedTeamName = Normalize(teamName);

        return (response?.Response ?? [])
            .Select(candidate => candidate.Team)
            .Where(team => team is not null && team.National && !string.IsNullOrWhiteSpace(team.Name))
            .OrderByDescending(team => Normalize(team!.Name!).Equals(normalizedTeamName, StringComparison.OrdinalIgnoreCase))
            .ThenBy(team => team!.Name)
            .FirstOrDefault();
    }

    private async Task<T?> SendAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("x-apisports-key", options.ApiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }

    private static bool StartsWithWord(string value, string query)
    {
        return value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(word => word.StartsWith(query, StringComparison.OrdinalIgnoreCase));
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

    private sealed record ApiSportsIndexedPlayer(string ExternalId, string Name, string NormalizedName, string? TeamName, string? Position, string? PhotoUrl);

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
