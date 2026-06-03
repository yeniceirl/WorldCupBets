using WorldCupBets.Domain.Common;

namespace WorldCupBets.Domain.Entities;

public sealed class Match : Entity
{
    private const int MatchBetCloseGraceMinutes = 5;

    private Match()
    {
    }

    private Match(MatchPhase phase, string homeTeamName, string awayTeamName, DateTime startsAtUtc, string venue, string? groupName = null)
    {
        Phase = phase;
        HomeTeamName = homeTeamName;
        AwayTeamName = awayTeamName;
        StartsAtUtc = startsAtUtc;
        Venue = venue;
        GroupName = groupName;
    }

    public MatchPhase Phase { get; private set; }

    public string HomeTeamName { get; private set; } = string.Empty;

    public string AwayTeamName { get; private set; } = string.Empty;

    public DateTime StartsAtUtc { get; private set; }

    public string Venue { get; private set; } = string.Empty;

    public string? GroupName { get; private set; }

    public string? SourceProvider { get; private set; }

    public string? SourceMatchId { get; private set; }

    public DateTime? SourceSyncedAtUtc { get; private set; }

    public MatchBetSelection? OfficialResult { get; private set; }

    public DateTime? SettledAtUtc { get; private set; }

    public int Version { get; private set; }

    public static Match Create(MatchPhase phase, string homeTeamName, string awayTeamName, DateTime startsAtUtc, string venue)
    {
        return new Match(phase, homeTeamName, awayTeamName, startsAtUtc, venue);
    }

    public static Match CreateGroupStageFixture(
        string groupName,
        string homeTeamName,
        string awayTeamName,
        DateTime startsAtUtc,
        string venue,
        string sourceProvider,
        string sourceMatchId,
        DateTime sourceSyncedAtUtc)
    {
        var match = new Match(MatchPhase.GroupStage, homeTeamName, awayTeamName, startsAtUtc, venue, groupName);
        match.SetSource(sourceProvider, sourceMatchId, sourceSyncedAtUtc);
        return match;
    }

    public void UpdateGroupStageFixture(
        DateTime startsAtUtc,
        string venue,
        string sourceProvider,
        string sourceMatchId,
        DateTime sourceSyncedAtUtc)
    {
        if (Phase != MatchPhase.GroupStage)
        {
            throw new InvalidOperationException("Only group stage fixtures can be updated from external football data.");
        }

        StartsAtUtc = startsAtUtc;
        Venue = venue;
        SetSource(sourceProvider, sourceMatchId, sourceSyncedAtUtc);
    }

    public void UpdateGroupStageFixtureMetadata(
        string venue,
        string sourceProvider,
        string sourceMatchId,
        DateTime sourceSyncedAtUtc)
    {
        if (Phase != MatchPhase.GroupStage)
        {
            throw new InvalidOperationException("Only group stage fixtures can be updated from external football data.");
        }

        Venue = venue;
        SetSource(sourceProvider, sourceMatchId, sourceSyncedAtUtc);
    }

    public string GetStageLabel()
    {
        return Phase switch
        {
            MatchPhase.GroupStage => "Group Stage",
            MatchPhase.RoundOf32 => "Round of 32",
            MatchPhase.RoundOf16 => "Round of 16",
            MatchPhase.Quarterfinals => "Quarterfinals",
            MatchPhase.Semifinals => "Semifinals",
            MatchPhase.ThirdPlace => "Third Place",
            MatchPhase.Final => "Final",
            _ => throw new InvalidOperationException($"Unsupported match phase '{Phase}'.")
        };
    }

    public int GetStakeAmountCc()
    {
        return Phase switch
        {
            MatchPhase.GroupStage => 5,
            MatchPhase.RoundOf32 => 10,
            MatchPhase.RoundOf16 => 15,
            MatchPhase.Quarterfinals => 20,
            MatchPhase.Semifinals => 30,
            MatchPhase.ThirdPlace => 20,
            MatchPhase.Final => 40,
            _ => throw new InvalidOperationException($"Unsupported match phase '{Phase}'.")
        };
    }

    public DateTime GetBettingClosesAtUtc()
    {
        return StartsAtUtc.AddMinutes(MatchBetCloseGraceMinutes);
    }

    public bool IsBettingOpenAt(DateTime nowUtc)
    {
        return nowUtc <= GetBettingClosesAtUtc();
    }

    public bool CanRecordOfficialResultAt(DateTime nowUtc)
    {
        return !IsBettingOpenAt(nowUtc);
    }

    public void RecordOfficialResult(MatchBetSelection officialResult, DateTime nowUtc)
    {
        if (!CanRecordOfficialResultAt(nowUtc))
        {
            throw new InvalidOperationException("The match betting window must be closed before recording an official result.");
        }

        if (SettledAtUtc.HasValue && OfficialResult != officialResult)
        {
            throw new InvalidOperationException("A settled match result cannot be changed.");
        }

        OfficialResult = officialResult;
    }

    public void MarkSettled(DateTime settledAtUtc)
    {
        if (!OfficialResult.HasValue)
        {
            throw new InvalidOperationException("An official result is required before settlement.");
        }

        SettledAtUtc ??= settledAtUtc;
    }

    private void SetSource(string sourceProvider, string sourceMatchId, DateTime sourceSyncedAtUtc)
    {
        SourceProvider = sourceProvider;
        SourceMatchId = sourceMatchId;
        SourceSyncedAtUtc = sourceSyncedAtUtc;
    }
}
