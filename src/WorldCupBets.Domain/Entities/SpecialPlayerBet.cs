using WorldCupBets.Domain.Common;

namespace WorldCupBets.Domain.Entities;

public sealed class SpecialPlayerBet : Entity
{
    private SpecialPlayerBet()
    {
    }

    private SpecialPlayerBet(int userId, SpecialPlayerBetCategory category, string playerName, string? externalPlayerId, decimal stakeAmountCc, DateTime placedAtUtc)
    {
        UserId = userId;
        Category = category;
        PlayerName = playerName;
        ExternalPlayerId = externalPlayerId;
        StakeAmountCc = stakeAmountCc;
        PlacedAtUtc = placedAtUtc;
    }

    public int UserId { get; private set; }

    public User? User { get; private set; }

    public SpecialPlayerBetCategory Category { get; private set; }

    public string PlayerName { get; private set; } = string.Empty;

    public string? ExternalPlayerId { get; private set; }

    public decimal StakeAmountCc { get; private set; }

    public DateTime PlacedAtUtc { get; private set; }

    public static SpecialPlayerBet Create(int userId, SpecialPlayerBetCategory category, string playerName, string? externalPlayerId, decimal stakeAmountCc, DateTime placedAtUtc)
    {
        return new SpecialPlayerBet(userId, category, playerName, externalPlayerId, stakeAmountCc, placedAtUtc);
    }
}
