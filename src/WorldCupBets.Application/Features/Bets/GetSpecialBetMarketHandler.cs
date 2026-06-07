using WorldCupBets.Domain.Repositories;

namespace WorldCupBets.Application.Features.Bets;

public sealed class GetSpecialBetMarketHandler
{
    public static async Task<SpecialBetMarketDto> Handle(
        GetSpecialBetMarketQuery query,
        IMatchRepository matchRepository,
        ITournamentPickRepository tournamentPickRepository,
        CancellationToken cancellationToken)
    {
        var closesAtUtc = await matchRepository.GetChampionBettingClosesAtUtcAsync(cancellationToken);
        var currentUserBets = await tournamentPickRepository.ListByUserAndCategoriesAsync(
            query.UserId,
            [Domain.Entities.TournamentPickCategory.BestPlayer, Domain.Entities.TournamentPickCategory.TopScorer],
            cancellationToken);
        var nowUtc = DateTime.UtcNow;

        return new SpecialBetMarketDto(
            PlaceSpecialPlayerBetHandler.SpecialPlayerBetStakeAmountCc,
            closesAtUtc,
            closesAtUtc is null || nowUtc < closesAtUtc.Value,
            currentUserBets
                .Select(bet => new SpecialPlayerBetDto(bet.Category.ToString(), bet.SelectedText, bet.ExternalId, bet.StakeAmountCc, bet.PlacedAtUtc))
                .ToArray());
    }
}
