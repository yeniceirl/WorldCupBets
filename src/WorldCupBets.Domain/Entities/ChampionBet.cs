using WorldCupBets.Domain.Common;

namespace WorldCupBets.Domain.Entities;

public sealed class ChampionBet : Entity
{
    private ChampionBet()
    {
    }

    private ChampionBet(int userId, string teamName, int stakeAmountCc, DateTime placedAtUtc)
    {
        UserId = userId;
        TeamName = teamName;
        StakeAmountCc = stakeAmountCc;
        PlacedAtUtc = placedAtUtc;
    }

    public int UserId { get; private set; }

    public User? User { get; private set; }

    public string TeamName { get; private set; } = string.Empty;

    public int StakeAmountCc { get; private set; }

    public DateTime PlacedAtUtc { get; private set; }

    public static ChampionBet Create(int userId, string teamName, int stakeAmountCc, DateTime placedAtUtc)
    {
        return new ChampionBet(userId, teamName, stakeAmountCc, placedAtUtc);
    }
}
