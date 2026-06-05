using WorldCupBets.Application.Abstractions;
using WorldCupBets.Domain.Common;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;

namespace WorldCupBets.Application.Features.Matches;

public sealed class RecordMatchResultHandler
{
    private const decimal CopaCoinScale = 100m;

    public static async Task<Result<RecordMatchResultDto>> Handle(
        RecordMatchResultCommand command,
        IMatchRepository matchRepository,
        IMatchBetRepository matchBetRepository,
        ITournamentSettlementRepository tournamentSettlementRepository,
        IUserRepository userRepository,
        IApplicationTransactionFactory transactionFactory,
        CancellationToken cancellationToken)
    {
        await using var transaction = await transactionFactory.BeginSerializableAsync(cancellationToken);

        var match = await matchRepository.GetByIdForSettlementAsync(command.MatchId, cancellationToken);
        if (match is null)
        {
            return Result<RecordMatchResultDto>.Failure(new Error("matches.not_found", "The match was not found."));
        }

        var nowUtc = DateTime.UtcNow;
        if (!match.CanRecordOfficialResultAt(nowUtc))
        {
            return Result<RecordMatchResultDto>.Failure(new Error("matches.result_window_open", "The match betting window must be closed before recording a result."));
        }

        if (match.SettledAtUtc.HasValue)
        {
            if (match.OfficialResult != command.OfficialResult)
            {
                return Result<RecordMatchResultDto>.Failure(new Error("matches.result_already_settled", "A settled match result cannot be changed."));
            }

            var currentSettlement = await tournamentSettlementRepository.GetOrCreateSingletonAsync(cancellationToken);
            return Result<RecordMatchResultDto>.Success(new RecordMatchResultDto(
                match.Id,
                match.OfficialResult.Value.ToString(),
                WasAlreadySettled: true,
                WinnersCount: 0,
                LosersCount: 0,
                ChampionJackpotContributionCc: 0,
                currentSettlement.ChampionJackpotCc,
                match.SettledAtUtc.Value));
        }

        match.RecordOfficialResult(command.OfficialResult, nowUtc);

        var bets = await matchBetRepository.ListByMatchForSettlementAsync(match.Id, cancellationToken);
        var winners = bets.Where(bet => bet.Selection == command.OfficialResult).ToArray();
        var losers = bets.Where(bet => bet.Selection != command.OfficialResult).ToArray();
        var jackpotContributionCc = ApplyPayouts(winners, losers);

        var settlement = await tournamentSettlementRepository.GetOrCreateSingletonAsync(cancellationToken);
        if (jackpotContributionCc > 0)
        {
            settlement.AddChampionJackpot(jackpotContributionCc);
        }

        match.MarkSettled(nowUtc);
        var settledAtUtc = match.SettledAtUtc ?? nowUtc;
        await userRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result<RecordMatchResultDto>.Success(new RecordMatchResultDto(
            match.Id,
            command.OfficialResult.ToString(),
            WasAlreadySettled: false,
            winners.Length,
            losers.Length,
            jackpotContributionCc,
            settlement.ChampionJackpotCc,
            settledAtUtc));
    }

    private static decimal ApplyPayouts(IReadOnlyCollection<MatchBet> winners, IReadOnlyCollection<MatchBet> losers)
    {
        if (winners.Count == 0)
        {
            var jackpotContributionCc = 0m;
            foreach (var loser in losers)
            {
                var refundCc = RoundDownToCents(loser.StakeAmountCc / 2m);
                var residualCc = loser.StakeAmountCc - refundCc;
                if (refundCc > 0)
                {
                    loser.User.CreditBalance(refundCc);
                }

                jackpotContributionCc = checked(jackpotContributionCc + residualCc);
            }

            return jackpotContributionCc;
        }

        if (losers.Count == 0)
        {
            foreach (var winner in winners)
            {
                winner.User.CreditBalance(winner.StakeAmountCc);
            }

            return 0m;
        }

        var losingPoolCc = losers.Sum(loser => loser.StakeAmountCc);
        var winnerProfitCc = RoundDownToCents(losingPoolCc / winners.Count);
        var jackpotRemainderCc = losingPoolCc - (winnerProfitCc * winners.Count);

        foreach (var winner in winners)
        {
            winner.User.CreditBalance(winner.StakeAmountCc + winnerProfitCc);
        }

        return jackpotRemainderCc;
    }

    private static decimal RoundDownToCents(decimal amountCc)
    {
        return Math.Floor(amountCc * CopaCoinScale) / CopaCoinScale;
    }
}
