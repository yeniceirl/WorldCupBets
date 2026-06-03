using WorldCupBets.Domain.Repositories;

namespace WorldCupBets.Application.Features.Leaderboard;

public sealed class GetLeaderboardHandler
{
    public static async Task<IReadOnlyList<LeaderboardItemDto>> Handle(
        GetLeaderboardQuery query,
        IUserRepository userRepository,
        CancellationToken cancellationToken)
    {
        var users = await userRepository.ListLeaderboardAsync(cancellationToken);

        return users
            .Select((user, index) => new LeaderboardItemDto(
                index + 1,
                user.DisplayName,
                user.CurrentBalanceCc))
            .ToArray();
    }
}
