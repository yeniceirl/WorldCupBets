using Microsoft.EntityFrameworkCore;
using WorldCupBets.Application.Abstractions;
using WorldCupBets.Domain.Entities;

namespace WorldCupBets.Infrastructure.Persistence.Repositories;

public sealed class ExternalFootballPlayerRepository(AppDbContext dbContext) : IExternalFootballPlayerRepository
{
    public async Task ReplacePlayersAsync(string providerName, IReadOnlyList<ExternalFootballPlayerDto> players, DateTime syncedAtUtc, CancellationToken cancellationToken = default)
    {
        var existingPlayers = await dbContext.ExternalFootballPlayers.Where(player => player.ProviderName == providerName).ToArrayAsync(cancellationToken);

        dbContext.ExternalFootballPlayers.RemoveRange(existingPlayers);
        dbContext.ExternalFootballPlayers.AddRange(players.Select(player => ExternalFootballPlayer.Create(
            providerName,
            player.ExternalId,
            player.Name,
            player.NormalizedName,
            player.TeamExternalId,
            player.TeamName,
            player.Position,
            player.PhotoUrl,
            syncedAtUtc)));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ExternalFootballPlayerDto>> SearchAsync(string providerName, string normalizedQuery, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return [];
        }

        var players = await dbContext.ExternalFootballPlayers
            .AsNoTracking()
            .Where(player => player.ProviderName == providerName && EF.Functions.Like(player.NormalizedName, $"%{normalizedQuery}%"))
            .ToArrayAsync(cancellationToken);

        return players
            .Select(player => new ExternalFootballPlayerDto(
                player.ExternalId,
                player.Name,
                player.NormalizedName,
                player.TeamExternalId,
                player.TeamName,
                player.Position,
                player.PhotoUrl))
            .ToArray();
    }

    public async Task<IReadOnlyDictionary<string, string>> GetTeamIdMapAsync(string providerName, CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.ExternalFootballPlayers
            .AsNoTracking()
            .Where(player => player.ProviderName == providerName && player.TeamName != null)
            .Select(player => new { player.TeamName, player.TeamExternalId })
            .Distinct()
            .ToArrayAsync(cancellationToken);

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            if (row.TeamName is not null && !map.ContainsKey(row.TeamName))
            {
                map[row.TeamName] = row.TeamExternalId;
            }
        }

        return map;
    }

    public async Task<IReadOnlyDictionary<string, string?>> GetPhotoUrlsByExternalIdsAsync(string providerName, IReadOnlyCollection<string> externalIds, CancellationToken cancellationToken = default)
    {
        if (externalIds.Count == 0)
        {
            return new Dictionary<string, string?>();
        }

        var rows = await dbContext.ExternalFootballPlayers
            .AsNoTracking()
            .Where(player => player.ProviderName == providerName && externalIds.Contains(player.ExternalId))
            .Select(player => new { player.ExternalId, player.PhotoUrl })
            .ToArrayAsync(cancellationToken);

        return rows.ToDictionary(row => row.ExternalId, row => row.PhotoUrl, StringComparer.OrdinalIgnoreCase);
    }
}
