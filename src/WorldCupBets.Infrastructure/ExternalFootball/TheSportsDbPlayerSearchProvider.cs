using System.Net.Http.Json;
using System.Text.Json.Serialization;
using WorldCupBets.Application.Abstractions;

namespace WorldCupBets.Infrastructure.ExternalFootball;

public sealed class TheSportsDbPlayerSearchProvider(HttpClient httpClient) : IPlayerSearchProvider
{
    public async Task<IReadOnlyList<PlayerSearchResultDto>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var normalizedQuery = query.Trim();
        if (normalizedQuery.Length < 3)
        {
            return [];
        }

        var response = await httpClient.GetFromJsonAsync<PlayerSearchResponse>(
            $"/api/v1/json/3/searchplayers.php?p={Uri.EscapeDataString(normalizedQuery)}",
            cancellationToken);

        return (response?.Players ?? [])
            .Where(player => string.Equals(player.Sport, "Soccer", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(player.Name))
            .Take(10)
            .Select(player => new PlayerSearchResultDto(
                player.Id ?? player.Name!,
                player.Name!,
                player.TeamName,
                player.Nationality,
                player.Position,
                player.ThumbnailUrl))
            .ToArray();
    }

    private sealed record PlayerSearchResponse([property: JsonPropertyName("player")] IReadOnlyList<TheSportsDbPlayer>? Players);

    private sealed record TheSportsDbPlayer(
        [property: JsonPropertyName("idPlayer")] string? Id,
        [property: JsonPropertyName("strPlayer")] string? Name,
        [property: JsonPropertyName("strTeam")] string? TeamName,
        [property: JsonPropertyName("strSport")] string? Sport,
        [property: JsonPropertyName("strNationality")] string? Nationality,
        [property: JsonPropertyName("strPosition")] string? Position,
        [property: JsonPropertyName("strThumb")] string? ThumbnailUrl);
}
