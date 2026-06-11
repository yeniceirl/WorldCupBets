using WorldCupBets.Domain.Common;

namespace WorldCupBets.Domain.Entities;

public sealed class ExternalFootballPlayer : Entity
{
    private ExternalFootballPlayer()
    {
    }

    private ExternalFootballPlayer(string providerName, string externalId, string name, string normalizedName, string teamExternalId, string? teamName, string? position, string? photoUrl, DateTime syncedAtUtc)
    {
        ProviderName = providerName;
        ExternalId = externalId;
        Name = name;
        NormalizedName = normalizedName;
        TeamExternalId = teamExternalId;
        TeamName = teamName;
        Position = position;
        PhotoUrl = photoUrl;
        SyncedAtUtc = syncedAtUtc;
    }

    public string ProviderName { get; private set; } = string.Empty;

    public string ExternalId { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public string TeamExternalId { get; private set; } = string.Empty;

    public string? TeamName { get; private set; }

    public string? Position { get; private set; }

    public string? PhotoUrl { get; private set; }

    public DateTime SyncedAtUtc { get; private set; }

    public static ExternalFootballPlayer Create(string providerName, string externalId, string name, string normalizedName, string teamExternalId, string? teamName, string? position, string? photoUrl, DateTime syncedAtUtc)
    {
        return new ExternalFootballPlayer(providerName, externalId, name, normalizedName, teamExternalId, teamName, position, photoUrl, syncedAtUtc);
    }
}
