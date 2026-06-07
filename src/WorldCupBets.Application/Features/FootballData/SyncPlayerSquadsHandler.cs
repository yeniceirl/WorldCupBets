using System.Globalization;
using System.Text;
using WorldCupBets.Application.Abstractions;

namespace WorldCupBets.Application.Features.FootballData;

public sealed class SyncPlayerSquadsHandler
{
    public static async Task<SyncPlayerSquadsResultDto> Handle(
        SyncPlayerSquadsCommand command,
        IPlayerSquadProvider playerSquadProvider,
        IExternalFootballPlayerRepository externalFootballPlayerRepository,
        ApiSportsFootballSyncOptions options,
        CancellationToken cancellationToken)
    {
        var providerName = playerSquadProvider.ProviderName;
        var syncedAtUtc = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return new SyncPlayerSquadsResultDto(providerName, NotConfigured: true, 0, 0, [], syncedAtUtc);
        }

        if (options.IncludedTeamNames.Count == 0)
        {
            return new SyncPlayerSquadsResultDto(providerName, NotConfigured: false, 0, 0, [], syncedAtUtc);
        }

        var teamIdMap = await externalFootballPlayerRepository.GetTeamIdMapAsync(providerName, cancellationToken);
        var indexedPlayers = new List<ExternalFootballPlayerDto>();
        var errors = new List<PlayerSquadSyncErrorDto>();
        var teamsProcessedCount = 0;
        var rateLimited = false;

        foreach (var teamName in options.IncludedTeamNames)
        {
            if (rateLimited)
            {
                break;
            }

            try
            {
                var teamExternalId = teamIdMap.TryGetValue(teamName, out var cachedTeamId)
                    ? cachedTeamId
                    : await playerSquadProvider.ResolveTeamIdAsync(teamName, cancellationToken);

                if (teamExternalId is null)
                {
                    errors.Add(new PlayerSquadSyncErrorDto(teamName, "Team could not be resolved.", RateLimited: false));
                    continue;
                }

                var squad = await playerSquadProvider.GetSquadAsync(teamExternalId, cancellationToken);

                indexedPlayers.AddRange(squad
                    .Where(member => !string.IsNullOrWhiteSpace(member.ExternalId) && !string.IsNullOrWhiteSpace(member.Name))
                    .Select(member => new ExternalFootballPlayerDto(
                        member.ExternalId,
                        member.Name,
                        Normalize(member.Name),
                        teamExternalId,
                        teamName,
                        member.Position,
                        member.PhotoUrl)));

                teamsProcessedCount++;
            }
            catch (ApiSportsRateLimitException exception)
            {
                errors.Add(new PlayerSquadSyncErrorDto(teamName, exception.Message, RateLimited: true));
                rateLimited = true;
            }
            catch (HttpRequestException exception)
            {
                errors.Add(new PlayerSquadSyncErrorDto(teamName, exception.Message, RateLimited: false));
            }
        }

        var distinctPlayers = indexedPlayers
            .GroupBy(player => player.ExternalId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();

        await externalFootballPlayerRepository.ReplacePlayersAsync(providerName, distinctPlayers, syncedAtUtc, cancellationToken);

        return new SyncPlayerSquadsResultDto(
            providerName,
            NotConfigured: false,
            teamsProcessedCount,
            distinctPlayers.Length,
            errors,
            syncedAtUtc);
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

/// <summary>
/// Configuration values the sync handler needs from API-Sports settings, kept independent of
/// the Infrastructure-layer options type so the Application layer has no Infrastructure dependency.
/// </summary>
public sealed record ApiSportsFootballSyncOptions(string ApiKey, IReadOnlySet<string> IncludedTeamNames);
