using WorldCupBets.Domain.Common;

namespace WorldCupBets.Domain.Entities;

public sealed class ExternalFootballGroupStanding : Entity
{
    private ExternalFootballGroupStanding()
    {
    }

    private ExternalFootballGroupStanding(string providerName, string groupName, string teamExternalId, int played, int won, int drawn, int lost, int goalsFor, int goalsAgainst, int goalDifference, int points, DateTime syncedAtUtc)
    {
        ProviderName = providerName;
        GroupName = groupName;
        TeamExternalId = teamExternalId;
        Played = played;
        Won = won;
        Drawn = drawn;
        Lost = lost;
        GoalsFor = goalsFor;
        GoalsAgainst = goalsAgainst;
        GoalDifference = goalDifference;
        Points = points;
        SyncedAtUtc = syncedAtUtc;
    }

    public string ProviderName { get; private set; } = string.Empty;

    public string GroupName { get; private set; } = string.Empty;

    public string TeamExternalId { get; private set; } = string.Empty;

    public int Played { get; private set; }

    public int Won { get; private set; }

    public int Drawn { get; private set; }

    public int Lost { get; private set; }

    public int GoalsFor { get; private set; }

    public int GoalsAgainst { get; private set; }

    public int GoalDifference { get; private set; }

    public int Points { get; private set; }

    public DateTime SyncedAtUtc { get; private set; }

    public static ExternalFootballGroupStanding Create(string providerName, string groupName, string teamExternalId, int played, int won, int drawn, int lost, int goalsFor, int goalsAgainst, int goalDifference, int points, DateTime syncedAtUtc)
    {
        return new ExternalFootballGroupStanding(providerName, groupName, teamExternalId, played, won, drawn, lost, goalsFor, goalsAgainst, goalDifference, points, syncedAtUtc);
    }
}
