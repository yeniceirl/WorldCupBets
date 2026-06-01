using WorldCupBets.Domain.Repositories;

namespace WorldCupBets.Application.Features.Matches;

public sealed class GetMatchesHandler
{
    public static async Task<IReadOnlyList<MatchListItemDto>> Handle(
        GetMatchesQuery query,
        IMatchRepository matchRepository,
        IMatchBetRepository matchBetRepository,
        CancellationToken cancellationToken)
    {
        var matches = await matchRepository.ListAsync(cancellationToken);
        var userBets = await matchBetRepository.ListByUserAsync(query.UserId, cancellationToken);
        var userSelectionsByMatchId = userBets.ToDictionary(matchBet => matchBet.MatchId, matchBet => matchBet.Selection.ToString());
        var nowUtc = DateTime.UtcNow;

        return matches
            .Select(match => new MatchListItemDto(
                match.Id,
                match.GetStageLabel(),
                match.HomeTeamName,
                match.AwayTeamName,
                match.StartsAtUtc,
                match.GetBettingClosesAtUtc(),
                match.IsBettingOpenAt(nowUtc),
                match.GetStakeAmountCc(),
                match.Venue,
                userSelectionsByMatchId.GetValueOrDefault(match.Id)))
            .ToArray();
    }
}
