using WorldCupBets.Domain.Common;

namespace WorldCupBets.Domain.Entities;

public sealed class ExternalFootballMatch : Entity
{
    private ExternalFootballMatch()
    {
    }

    private ExternalFootballMatch(string providerName, string externalId, string? homeTeamExternalId, string? awayTeamExternalId, string? homeTeamNameEn, string? awayTeamNameEn, string? homeTeamLabel, string? awayTeamLabel, string groupName, string matchday, string localDateText, string stadiumExternalId, bool isFinished, string timeElapsed, string stageType, int? homeScore, int? awayScore, DateTime syncedAtUtc)
    {
        ProviderName = providerName;
        ExternalId = externalId;
        HomeTeamExternalId = homeTeamExternalId;
        AwayTeamExternalId = awayTeamExternalId;
        HomeTeamNameEn = homeTeamNameEn;
        AwayTeamNameEn = awayTeamNameEn;
        HomeTeamLabel = homeTeamLabel;
        AwayTeamLabel = awayTeamLabel;
        GroupName = groupName;
        Matchday = matchday;
        LocalDateText = localDateText;
        StadiumExternalId = stadiumExternalId;
        IsFinished = isFinished;
        TimeElapsed = timeElapsed;
        StageType = stageType;
        HomeScore = homeScore;
        AwayScore = awayScore;
        SyncedAtUtc = syncedAtUtc;
    }

    public string ProviderName { get; private set; } = string.Empty;

    public string ExternalId { get; private set; } = string.Empty;

    public string? HomeTeamExternalId { get; private set; }

    public string? AwayTeamExternalId { get; private set; }

    public string? HomeTeamNameEn { get; private set; }

    public string? AwayTeamNameEn { get; private set; }

    public string? HomeTeamLabel { get; private set; }

    public string? AwayTeamLabel { get; private set; }

    public string GroupName { get; private set; } = string.Empty;

    public string Matchday { get; private set; } = string.Empty;

    public string LocalDateText { get; private set; } = string.Empty;

    public string StadiumExternalId { get; private set; } = string.Empty;

    public bool IsFinished { get; private set; }

    public string TimeElapsed { get; private set; } = string.Empty;

    public string StageType { get; private set; } = string.Empty;

    public int? HomeScore { get; private set; }

    public int? AwayScore { get; private set; }

    public DateTime SyncedAtUtc { get; private set; }

    public static ExternalFootballMatch Create(string providerName, string externalId, string? homeTeamExternalId, string? awayTeamExternalId, string? homeTeamNameEn, string? awayTeamNameEn, string? homeTeamLabel, string? awayTeamLabel, string groupName, string matchday, string localDateText, string stadiumExternalId, bool isFinished, string timeElapsed, string stageType, int? homeScore, int? awayScore, DateTime syncedAtUtc)
    {
        return new ExternalFootballMatch(providerName, externalId, homeTeamExternalId, awayTeamExternalId, homeTeamNameEn, awayTeamNameEn, homeTeamLabel, awayTeamLabel, groupName, matchday, localDateText, stadiumExternalId, isFinished, timeElapsed, stageType, homeScore, awayScore, syncedAtUtc);
    }
}
