using System.Globalization;
using System.Text;
using WorldCupBets.Application.Abstractions;

namespace WorldCupBets.Infrastructure.ExternalFootball;

public sealed class ApiSportsFootballPlayerSearchProvider(
    IExternalFootballPlayerRepository externalFootballPlayerRepository) : IPlayerSearchProvider
{
    public async Task<IReadOnlyList<PlayerSearchResultDto>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var normalizedQuery = Normalize(query);
        if (normalizedQuery.Length < 3)
        {
            return [];
        }

        var players = await externalFootballPlayerRepository.SearchAsync(ApiSportsPlayerSquadProvider.Provider, normalizedQuery, cancellationToken);

        return players
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
}
