using WorldCupBets.Domain.Repositories;

namespace WorldCupBets.Application.Features.Leaderboard;

public sealed class GetLeaderboardHandler
{
    public static async Task<IReadOnlyList<LeaderboardItemDto>> Handle(
        GetLeaderboardQuery query,
        IUserRepository userRepository,
        IMatchBetRepository matchBetRepository,
        ITournamentPickRepository tournamentPickRepository,
        ITournamentSettlementRepository tournamentSettlementRepository,
        CancellationToken cancellationToken)
    {
        var users = await userRepository.ListLeaderboardAsync(cancellationToken);
        var pendingMatchStakesByUser = await matchBetRepository.ListPendingStakeAmountsByUserAsync(cancellationToken);
        var championSettled = await tournamentSettlementRepository.IsChampionSettledAsync(cancellationToken);
        var pendingChampionStakesByUser = championSettled
            ? new Dictionary<int, decimal>()
            : await tournamentPickRepository.ListStakeAmountsByUserAsync([Domain.Entities.TournamentPickCategory.Champion], cancellationToken);
        var pendingSpecialPlayerStakesByUser = await tournamentPickRepository.ListStakeAmountsByUserAsync(
            [Domain.Entities.TournamentPickCategory.BestPlayer, Domain.Entities.TournamentPickCategory.TopScorer],
            cancellationToken);

        return users
            .Select(user =>
            {
                var pendingStakeAmountCc = pendingMatchStakesByUser.GetValueOrDefault(user.Id)
                    + pendingChampionStakesByUser.GetValueOrDefault(user.Id)
                    + pendingSpecialPlayerStakesByUser.GetValueOrDefault(user.Id);

                return new
                {
                    user.DisplayName,
                    AvailableBalanceCc = user.CurrentBalanceCc,
                    CurrentBalanceCc = checked(user.CurrentBalanceCc + pendingStakeAmountCc),
                    PendingStakeAmountCc = pendingStakeAmountCc
                };
            })
            .OrderByDescending(item => item.CurrentBalanceCc)
            .ThenBy(item => item.DisplayName)
            .Select((item, index) => new LeaderboardItemDto(
                index + 1,
                item.DisplayName,
                item.CurrentBalanceCc,
                item.PendingStakeAmountCc,
                item.AvailableBalanceCc))
            .ToArray();
    }
}
