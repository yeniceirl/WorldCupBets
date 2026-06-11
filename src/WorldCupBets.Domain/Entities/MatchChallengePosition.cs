using WorldCupBets.Domain.Common;

namespace WorldCupBets.Domain.Entities;

public sealed class MatchChallengePosition : Entity
{
    private MatchChallengePosition()
    {
    }

    private MatchChallengePosition(int userId, MatchChallengeSide side, decimal stakeAmountCc, DateTime escrowedAtUtc)
    {
        UserId = userId;
        Side = side;
        StakeAmountCc = stakeAmountCc;
        EscrowedAtUtc = escrowedAtUtc;
    }

    public int MatchChallengeId { get; private set; }

    public MatchChallenge MatchChallenge { get; private set; } = null!;

    public int UserId { get; private set; }

    public User User { get; private set; } = null!;

    public MatchChallengeSide Side { get; private set; }

    public decimal StakeAmountCc { get; private set; }

    public DateTime EscrowedAtUtc { get; private set; }

    internal static MatchChallengePosition Create(int userId, MatchChallengeSide side, decimal stakeAmountCc, DateTime escrowedAtUtc)
    {
        if (stakeAmountCc <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stakeAmountCc), "Stake amount must be greater than zero.");
        }

        return new MatchChallengePosition(userId, side, stakeAmountCc, escrowedAtUtc);
    }
}
