using WorldCupBets.Domain.Common;

namespace WorldCupBets.Domain.Entities;

public sealed class Match : Entity
{
    private const int MatchBetCloseGraceMinutes = 5;

    private Match()
    {
    }

    private Match(MatchPhase phase, string homeTeamName, string awayTeamName, DateTime startsAtUtc, string venue)
    {
        Phase = phase;
        HomeTeamName = homeTeamName;
        AwayTeamName = awayTeamName;
        StartsAtUtc = startsAtUtc;
        Venue = venue;
    }

    public MatchPhase Phase { get; private set; }

    public string HomeTeamName { get; private set; } = string.Empty;

    public string AwayTeamName { get; private set; } = string.Empty;

    public DateTime StartsAtUtc { get; private set; }

    public string Venue { get; private set; } = string.Empty;

    public static Match Create(MatchPhase phase, string homeTeamName, string awayTeamName, DateTime startsAtUtc, string venue)
    {
        return new Match(phase, homeTeamName, awayTeamName, startsAtUtc, venue);
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
}
