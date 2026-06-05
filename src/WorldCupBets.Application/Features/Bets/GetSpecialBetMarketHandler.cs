using WorldCupBets.Domain.Repositories;

namespace WorldCupBets.Application.Features.Bets;

public sealed class GetSpecialBetMarketHandler
{
    public static async Task<SpecialBetMarketDto> Handle(
        GetSpecialBetMarketQuery query,
        IMatchRepository matchRepository,
        ISpecialPlayerBetRepository specialPlayerBetRepository,
        CancellationToken cancellationToken)
    {
        var closesAtUtc = await matchRepository.GetChampionBettingClosesAtUtcAsync(cancellationToken);
        var currentUserBets = await specialPlayerBetRepository.ListByUserAsync(query.UserId, cancellationToken);
        var nowUtc = DateTime.UtcNow;

        return new SpecialBetMarketDto(
            PlaceSpecialPlayerBetHandler.SpecialPlayerBetStakeAmountCc,
            closesAtUtc,
            closesAtUtc is null || nowUtc < closesAtUtc.Value,
            currentUserBets
                .Select(bet => new SpecialPlayerBetDto(bet.Category.ToString(), bet.PlayerName, bet.ExternalPlayerId, bet.StakeAmountCc, bet.PlacedAtUtc))
                .ToArray());
    }
}
