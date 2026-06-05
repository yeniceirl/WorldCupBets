namespace WorldCupBets.Application.Features.Leaderboard;

public sealed record LeaderboardItemDto(
    int Rank,
    string DisplayName,
    decimal CurrentBalanceCc,
    decimal PendingStakeAmountCc,
    decimal AvailableBalanceCc);
