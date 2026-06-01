using WorldCupBets.Domain.Repositories;

namespace WorldCupBets.Application.Features.Bets;

public sealed class GetChampionBetMarketHandler
{
    public static async Task<ChampionBetMarketDto> Handle(
        GetChampionBetMarketQuery query,
        IMatchRepository matchRepository,
        IChampionBetRepository championBetRepository,
        CancellationToken cancellationToken)
    {
        var closesAtUtc = await matchRepository.GetChampionBettingClosesAtUtcAsync(cancellationToken);
        var currentUserBet = await championBetRepository.GetByUserAsync(query.UserId, cancellationToken);
        var teamOptions = await matchRepository.ListTeamNamesAsync(cancellationToken);
        var nowUtc = DateTime.UtcNow;

        return new ChampionBetMarketDto(
            teamOptions,
            PlaceChampionBetHandler.ChampionBetStakeAmountCc,
            closesAtUtc,
            closesAtUtc is null || nowUtc < closesAtUtc.Value,
            currentUserBet?.TeamName);
    }
}
