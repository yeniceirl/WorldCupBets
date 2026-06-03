using WorldCupBets.Domain.Common;

namespace WorldCupBets.Domain.Entities;

public sealed class ExternalFootballTeam : Entity
{
    private ExternalFootballTeam()
    {
    }

    private ExternalFootballTeam(string providerName, string externalId, string nameEn, string fifaCode, string? iso2, string? groupName, string? flagUrl, DateTime syncedAtUtc)
    {
        ProviderName = providerName;
        ExternalId = externalId;
        NameEn = nameEn;
        FifaCode = fifaCode;
        Iso2 = iso2;
        GroupName = groupName;
        FlagUrl = flagUrl;
        SyncedAtUtc = syncedAtUtc;
    }

    public string ProviderName { get; private set; } = string.Empty;

    public string ExternalId { get; private set; } = string.Empty;

    public string NameEn { get; private set; } = string.Empty;

    public string FifaCode { get; private set; } = string.Empty;

    public string? Iso2 { get; private set; }

    public string? GroupName { get; private set; }

    public string? FlagUrl { get; private set; }

    public DateTime SyncedAtUtc { get; private set; }

    public static ExternalFootballTeam Create(string providerName, string externalId, string nameEn, string fifaCode, string? iso2, string? groupName, string? flagUrl, DateTime syncedAtUtc)
    {
        return new ExternalFootballTeam(providerName, externalId, nameEn, fifaCode, iso2, groupName, flagUrl, syncedAtUtc);
    }
}
