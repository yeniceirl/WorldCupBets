using WorldCupBets.Domain.Common;

namespace WorldCupBets.Domain.Entities;

public sealed class ExternalFootballStadium : Entity
{
    private ExternalFootballStadium()
    {
    }

    private ExternalFootballStadium(string providerName, string externalId, string nameEn, string? fifaName, string? cityEn, string? countryEn, int? capacity, string? region, DateTime syncedAtUtc)
    {
        ProviderName = providerName;
        ExternalId = externalId;
        NameEn = nameEn;
        FifaName = fifaName;
        CityEn = cityEn;
        CountryEn = countryEn;
        Capacity = capacity;
        Region = region;
        SyncedAtUtc = syncedAtUtc;
    }

    public string ProviderName { get; private set; } = string.Empty;

    public string ExternalId { get; private set; } = string.Empty;

    public string NameEn { get; private set; } = string.Empty;

    public string? FifaName { get; private set; }

    public string? CityEn { get; private set; }

    public string? CountryEn { get; private set; }

    public int? Capacity { get; private set; }

    public string? Region { get; private set; }

    public DateTime SyncedAtUtc { get; private set; }

    public static ExternalFootballStadium Create(string providerName, string externalId, string nameEn, string? fifaName, string? cityEn, string? countryEn, int? capacity, string? region, DateTime syncedAtUtc)
    {
        return new ExternalFootballStadium(providerName, externalId, nameEn, fifaName, cityEn, countryEn, capacity, region, syncedAtUtc);
    }
}
