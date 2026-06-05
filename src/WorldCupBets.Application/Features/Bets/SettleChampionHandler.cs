using WorldCupBets.Application.Abstractions;
using WorldCupBets.Domain.Common;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;

namespace WorldCupBets.Application.Features.Bets;

public sealed class SettleChampionHandler
{
    private const decimal CopaCoinScale = 100m;

    public static async Task<Result<SettleChampionResultDto>> Handle(
        SettleChampionCommand command,
        IChampionBetRepository championBetRepository,
        ITournamentSettlementRepository tournamentSettlementRepository,
        IUserRepository userRepository,
        IApplicationTransactionFactory transactionFactory,
        CancellationToken cancellationToken)
    {
        await using var transaction = await transactionFactory.BeginSerializableAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(command.ChampionTeamName))
        {
            return Result<SettleChampionResultDto>.Failure(new Error("bets.champion_required", "Champion team name is required."));
        }

        var championTeamName = command.ChampionTeamName.Trim();
        var settlement = await tournamentSettlementRepository.GetOrCreateSingletonAsync(cancellationToken);

        if (settlement.ChampionSettledAtUtc.HasValue)
        {
            if (!string.Equals(settlement.ChampionTeamName, championTeamName, StringComparison.OrdinalIgnoreCase))
            {
                return Result<SettleChampionResultDto>.Failure(new Error("bets.champion_already_settled", "Champion settlement cannot be changed."));
            }

            return Result<SettleChampionResultDto>.Success(new SettleChampionResultDto(
                settlement.ChampionTeamName ?? championTeamName,
                WasAlreadySettled: true,
                WinnersCount: 0,
                LosersCount: 0,
                settlement.ChampionJackpotCc,
                LosingStakePoolCc: 0,
                ProfitSharePerWinnerCc: 0,
                TotalPayoutPerWinnerCc: 0,
                settlement.UndistributedJackpotCc,
                settlement.ChampionSettledAtUtc.Value));
        }

        var bets = await championBetRepository.ListForSettlementAsync(cancellationToken);
        var winners = bets
            .Where(bet => string.Equals(bet.TeamName, championTeamName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var losers = bets
            .Where(bet => !string.Equals(bet.TeamName, championTeamName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var losingStakePoolCc = losers.Sum(bet => bet.StakeAmountCc);
        var distributableProfitPoolCc = checked(losingStakePoolCc + settlement.ChampionJackpotCc);
        var profitSharePerWinnerCc = winners.Length == 0 ? 0m : RoundDownToCents(distributableProfitPoolCc / winners.Length);
        var undistributedJackpotCc = winners.Length == 0 ? distributableProfitPoolCc : distributableProfitPoolCc - (profitSharePerWinnerCc * winners.Length);

        foreach (var winner in winners)
        {
            winner.User!.CreditBalance(winner.StakeAmountCc + profitSharePerWinnerCc);
        }

        var nowUtc = DateTime.UtcNow;
        settlement.MarkChampionSettled(championTeamName, nowUtc, undistributedJackpotCc);
        await userRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result<SettleChampionResultDto>.Success(new SettleChampionResultDto(
            settlement.ChampionTeamName ?? championTeamName,
            WasAlreadySettled: false,
            winners.Length,
            losers.Length,
            settlement.ChampionJackpotCc,
            losingStakePoolCc,
            profitSharePerWinnerCc,
            PlaceChampionBetHandler.ChampionBetStakeAmountCc + profitSharePerWinnerCc,
            settlement.UndistributedJackpotCc,
            settlement.ChampionSettledAtUtc ?? nowUtc));
    }

    private static decimal RoundDownToCents(decimal amountCc)
    {
        return Math.Floor(amountCc * CopaCoinScale) / CopaCoinScale;
    }
}
