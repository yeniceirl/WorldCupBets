using WorldCupBets.Application.Abstractions;
using WorldCupBets.Domain.Common;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;

namespace WorldCupBets.Application.Features.Bets;

public sealed class PlaceSpecialPlayerBetHandler
{
    public const decimal SpecialPlayerBetStakeAmountCc = 50m;

    public static async Task<Result<PlaceSpecialPlayerBetResultDto>> Handle(
        PlaceSpecialPlayerBetCommand command,
        IUserRepository userRepository,
        IMatchRepository matchRepository,
        ISpecialPlayerBetRepository specialPlayerBetRepository,
        IApplicationTransactionFactory transactionFactory,
        CancellationToken cancellationToken)
    {
        await using var transaction = await transactionFactory.BeginSerializableAsync(cancellationToken);

        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return Result<PlaceSpecialPlayerBetResultDto>.Failure(new Error("bets.user_not_found", "The bettor was not found."));
        }

        var playerName = command.PlayerName.Trim();
        if (playerName.Length < 3)
        {
            return Result<PlaceSpecialPlayerBetResultDto>.Failure(new Error("bets.invalid_player_name", "Player name must have at least 3 characters."));
        }

        if (await specialPlayerBetRepository.ExistsForUserAndCategoryAsync(command.UserId, command.Category, cancellationToken))
        {
            return Result<PlaceSpecialPlayerBetResultDto>.Failure(new Error("bets.special_player_bet_already_exists", "You already placed this player bet."));
        }

        var closesAtUtc = await matchRepository.GetChampionBettingClosesAtUtcAsync(cancellationToken);
        var nowUtc = DateTime.UtcNow;
        if (closesAtUtc is not null && nowUtc >= closesAtUtc.Value)
        {
            return Result<PlaceSpecialPlayerBetResultDto>.Failure(new Error("bets.special_betting_closed", "Tournament special betting is already closed."));
        }

        if (!user.CanAfford(SpecialPlayerBetStakeAmountCc))
        {
            return Result<PlaceSpecialPlayerBetResultDto>.Failure(new Error("bets.insufficient_balance", "You do not have enough CopaCoin to place this bet."));
        }

        var externalPlayerId = string.IsNullOrWhiteSpace(command.ExternalPlayerId) ? null : command.ExternalPlayerId.Trim();
        user.DeductBalance(SpecialPlayerBetStakeAmountCc);
        user.ApplyDeadRescueIfEligible();

        var specialPlayerBet = SpecialPlayerBet.Create(command.UserId, command.Category, playerName, externalPlayerId, SpecialPlayerBetStakeAmountCc, nowUtc);
        await specialPlayerBetRepository.AddAsync(specialPlayerBet, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result<PlaceSpecialPlayerBetResultDto>.Success(new PlaceSpecialPlayerBetResultDto(
            command.Category.ToString(),
            playerName,
            externalPlayerId,
            SpecialPlayerBetStakeAmountCc,
            user.CurrentBalanceCc,
            nowUtc));
    }
}
