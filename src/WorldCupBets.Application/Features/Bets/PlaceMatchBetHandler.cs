using WorldCupBets.Domain.Common;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;

namespace WorldCupBets.Application.Features.Bets;

public sealed class PlaceMatchBetHandler
{
    public static async Task<Result<PlaceMatchBetResultDto>> Handle(
        PlaceMatchBetCommand command,
        IUserRepository userRepository,
        IMatchRepository matchRepository,
        IMatchBetRepository matchBetRepository,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return Result<PlaceMatchBetResultDto>.Failure(new Error("bets.user_not_found", "The bettor was not found."));
        }

        var match = await matchRepository.GetByIdAsync(command.MatchId, cancellationToken);
        if (match is null)
        {
            return Result<PlaceMatchBetResultDto>.Failure(new Error("bets.match_not_found", "The selected match was not found."));
        }

        if (await matchBetRepository.ExistsForUserAndMatchAsync(command.UserId, command.MatchId, cancellationToken))
        {
            return Result<PlaceMatchBetResultDto>.Failure(new Error("bets.match_bet_already_exists", "You already placed a bet for this match."));
        }

        var nowUtc = DateTime.UtcNow;
        if (!match.IsBettingOpenAt(nowUtc))
        {
            return Result<PlaceMatchBetResultDto>.Failure(new Error("bets.match_betting_closed", "Betting for this match is already closed."));
        }

        var stakeAmountCc = match.GetStakeAmountCc();
        if (!user.CanAfford(stakeAmountCc))
        {
            return Result<PlaceMatchBetResultDto>.Failure(new Error("bets.insufficient_balance", "You do not have enough CopaCoin to place this bet."));
        }

        user.DeductBalance(stakeAmountCc);
        user.ApplyDeadRescueIfEligible();
        var matchBet = MatchBet.Create(command.UserId, command.MatchId, command.Selection, stakeAmountCc, nowUtc);
        await matchBetRepository.AddAsync(matchBet, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        return Result<PlaceMatchBetResultDto>.Success(new PlaceMatchBetResultDto(
            command.MatchId,
            command.Selection.ToString(),
            stakeAmountCc,
            user.CurrentBalanceCc,
            nowUtc));
    }
}
