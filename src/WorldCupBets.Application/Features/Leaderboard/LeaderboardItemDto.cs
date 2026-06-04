namespace WorldCupBets.Application.Features.Leaderboard;

public sealed record LeaderboardItemDto(
    int Rank,
    string DisplayName,
    int CurrentBalanceCc,
    int PendingStakeAmountCc);
