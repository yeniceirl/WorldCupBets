namespace WorldCupBets.Application.Abstractions;

/// <summary>
/// Fetches national-team squads from the external player-data provider so they can be persisted
/// by an admin-triggered sync. Implementations MUST throw <see cref="ApiSportsRateLimitException"/>
/// when the upstream API responds with HTTP 429, allowing callers to abort the whole sync.
/// </summary>
public interface IPlayerSquadProvider
{
    string ProviderName { get; }

    /// <summary>
    /// Resolves the external team id for the given national-team name.
    /// Returns <c>null</c> when no matching team is found.
    /// </summary>
    Task<string?> ResolveTeamIdAsync(string teamName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the squad for the given external team id.
    /// </summary>
    Task<IReadOnlyList<PlayerSquadMemberDto>> GetSquadAsync(string teamExternalId, CancellationToken cancellationToken = default);
}

public sealed record PlayerSquadMemberDto(
    string ExternalId,
    string Name,
    string? TeamName,
    string? Position,
    string? PhotoUrl);
