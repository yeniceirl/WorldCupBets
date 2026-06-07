namespace WorldCupBets.Application.Abstractions;

public interface IExternalFootballPlayerRepository
{
    Task ReplacePlayersAsync(string providerName, IReadOnlyList<ExternalFootballPlayerDto> players, DateTime syncedAtUtc, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExternalFootballPlayerDto>> SearchAsync(string providerName, string normalizedQuery, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, string>> GetTeamIdMapAsync(string providerName, CancellationToken cancellationToken = default);
}

public sealed record ExternalFootballPlayerDto(
    string ExternalId,
    string Name,
    string NormalizedName,
    string TeamExternalId,
    string? TeamName,
    string? Position,
    string? PhotoUrl);
