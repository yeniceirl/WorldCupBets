using WorldCupBets.Domain.Repositories;

namespace WorldCupBets.Application.Features.Bets;

public sealed class GetChampionBetMarketHandler
{
    public static async Task<ChampionBetMarketDto> Handle(
        GetChampionBetMarketQuery query,
        IMatchRepository matchRepository,
        ITournamentPickRepository tournamentPickRepository,
        ITournamentSettlementRepository tournamentSettlementRepository,
        CancellationToken cancellationToken)
    {
        var closesAtUtc = await matchRepository.GetChampionBettingClosesAtUtcAsync(cancellationToken);
        var currentUserBet = await tournamentPickRepository.GetByUserAndCategoryAsync(query.UserId, Domain.Entities.TournamentPickCategory.Champion, cancellationToken);
        var teamOptions = await matchRepository.ListTeamNamesAsync(cancellationToken);
        var isSettled = await tournamentSettlementRepository.IsChampionSettledAsync(cancellationToken);
        var nowUtc = DateTime.UtcNow;

        return new ChampionBetMarketDto(
            teamOptions,
            PlaceChampionBetHandler.ChampionBetStakeAmountCc,
            closesAtUtc,
            closesAtUtc is null || nowUtc < closesAtUtc.Value,
            isSettled,
            currentUserBet?.SelectedText);
    }
}
