using WorldCupBets.Domain.Common;

namespace WorldCupBets.Domain.Entities;

public sealed class MatchBet : Entity
{
    private MatchBet()
    {
    }

    private MatchBet(int userId, int matchId, MatchBetSelection selection, int stakeAmountCc, DateTime placedAtUtc)
    {
        UserId = userId;
        MatchId = matchId;
        Selection = selection;
        StakeAmountCc = stakeAmountCc;
        PlacedAtUtc = placedAtUtc;
    }

    public int UserId { get; private set; }

    public User User { get; private set; } = null!;

    public int MatchId { get; private set; }

    public Match Match { get; private set; } = null!;

    public MatchBetSelection Selection { get; private set; }

    public int StakeAmountCc { get; private set; }

    public DateTime PlacedAtUtc { get; private set; }

    public static MatchBet Create(int userId, int matchId, MatchBetSelection selection, int stakeAmountCc, DateTime placedAtUtc)
    {
        if (stakeAmountCc <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stakeAmountCc), "Stake amount must be greater than zero.");
        }

        return new MatchBet(userId, matchId, selection, stakeAmountCc, placedAtUtc);
    }
}
