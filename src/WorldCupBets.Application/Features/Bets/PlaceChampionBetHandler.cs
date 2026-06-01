using WorldCupBets.Domain.Common;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;

namespace WorldCupBets.Application.Features.Bets;

public sealed class PlaceChampionBetHandler
{
    public const int ChampionBetStakeAmountCc = 50;

    public static async Task<Result<PlaceChampionBetResultDto>> Handle(
        PlaceChampionBetCommand command,
        IUserRepository userRepository,
        IMatchRepository matchRepository,
        IChampionBetRepository championBetRepository,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return Result<PlaceChampionBetResultDto>.Failure(new Error("bets.user_not_found", "The bettor was not found."));
        }

        var teamOptions = await matchRepository.ListTeamNamesAsync(cancellationToken);
        if (!teamOptions.Contains(command.TeamName, StringComparer.OrdinalIgnoreCase))
        {
            return Result<PlaceChampionBetResultDto>.Failure(new Error("bets.invalid_champion_team", "The selected champion team is not available."));
        }

        if (await championBetRepository.ExistsForUserAsync(command.UserId, cancellationToken))
        {
            return Result<PlaceChampionBetResultDto>.Failure(new Error("bets.champion_bet_already_exists", "You already placed a champion bet."));
        }

        var closesAtUtc = await matchRepository.GetChampionBettingClosesAtUtcAsync(cancellationToken);
        var nowUtc = DateTime.UtcNow;
        if (closesAtUtc is not null && nowUtc >= closesAtUtc.Value)
        {
            return Result<PlaceChampionBetResultDto>.Failure(new Error("bets.champion_betting_closed", "Champion betting is already closed."));
        }

        if (!user.CanAfford(ChampionBetStakeAmountCc))
        {
            return Result<PlaceChampionBetResultDto>.Failure(new Error("bets.insufficient_balance", "You do not have enough CopaCoin to place this bet."));
        }

        user.DeductBalance(ChampionBetStakeAmountCc);
        user.ApplyDeadRescueIfEligible();

        var normalizedTeamName = teamOptions.Single(teamName => string.Equals(teamName, command.TeamName, StringComparison.OrdinalIgnoreCase));
        var championBet = ChampionBet.Create(command.UserId, normalizedTeamName, ChampionBetStakeAmountCc, nowUtc);
        await championBetRepository.AddAsync(championBet, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        return Result<PlaceChampionBetResultDto>.Success(new PlaceChampionBetResultDto(
            normalizedTeamName,
            ChampionBetStakeAmountCc,
            user.CurrentBalanceCc,
            nowUtc));
    }
}
