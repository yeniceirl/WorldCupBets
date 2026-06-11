using WorldCupBets.Application.Abstractions;
using WorldCupBets.Domain.Common;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;

namespace WorldCupBets.Application.Features.Challenges;

public sealed class SettleChallengeHandler
{
    public static async Task<Result<ChallengeDto>> Handle(
        SettleChallengeCommand command,
        IUserRepository userRepository,
        IMatchChallengeRepository challengeRepository,
        IApplicationTransactionFactory transactionFactory,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(command.WinnerSide))
        {
            return Result<ChallengeDto>.Failure(new Error("challenges.invalid_payload", "Winner side must be Creator or Taker."));
        }

        await using var transaction = await transactionFactory.BeginSerializableAsync(cancellationToken);

        var challenge = await challengeRepository.GetForUpdateAsync(command.ChallengeId, cancellationToken);
        if (challenge is null)
        {
            return Result<ChallengeDto>.Failure(new Error("challenges.not_found", "The challenge was not found."));
        }

        if (challenge.Status != MatchChallengeStatus.Matched)
        {
            return Result<ChallengeDto>.Failure(new Error("challenges.not_matched", "Only matched challenges can be settled."));
        }

        var winnerPosition = challenge.Positions.Single(position => position.Side == command.WinnerSide);
        var winner = await userRepository.GetByIdAsync(winnerPosition.UserId, cancellationToken);
        if (winner is null)
        {
            return Result<ChallengeDto>.Failure(new Error("challenges.participant_not_found", "The winning challenge participant was not found."));
        }

        winner.CreditBalance(challenge.Positions.Sum(position => position.StakeAmountCc));
        challenge.Settle(command.WinnerSide, DateTime.UtcNow);
        await userRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result<ChallengeDto>.Success(ChallengeDto.FromEntity(challenge));
    }
}

public sealed class VoidChallengeHandler
{
    public static Task<Result<ChallengeDto>> Handle(
        VoidChallengeCommand command,
        IUserRepository userRepository,
        IMatchChallengeRepository challengeRepository,
        IApplicationTransactionFactory transactionFactory,
        CancellationToken cancellationToken)
    {
        return ChallengeLifecycle.RefundAndCloseAsync(command.ChallengeId, MatchChallengeStatus.Voided, userRepository, challengeRepository, transactionFactory, cancellationToken);
    }
}

public sealed class CancelChallengeHandler
{
    public static async Task<Result<ChallengeMutationResultDto>> Handle(
        CancelChallengeCommand command,
        IUserRepository userRepository,
        IMatchChallengeRepository challengeRepository,
        IApplicationTransactionFactory transactionFactory,
        CancellationToken cancellationToken)
    {
        await using var transaction = await transactionFactory.BeginSerializableAsync(cancellationToken);

        var challenge = await challengeRepository.GetForUpdateAsync(command.ChallengeId, cancellationToken);
        if (challenge is null)
        {
            return Result<ChallengeMutationResultDto>.Failure(new Error("challenges.not_found", "The challenge was not found."));
        }

        if (challenge.CreatorPosition.UserId != command.CreatorUserId)
        {
            return Result<ChallengeMutationResultDto>.Failure(new Error("challenges.not_creator", "Only the challenge creator can cancel it."));
        }

        if (challenge.Status != MatchChallengeStatus.Open)
        {
            return Result<ChallengeMutationResultDto>.Failure(new Error("challenges.not_open", "Only open challenges can be canceled."));
        }

        var creator = await userRepository.GetByIdAsync(command.CreatorUserId, cancellationToken);
        if (creator is null)
        {
            return Result<ChallengeMutationResultDto>.Failure(new Error("challenges.user_not_found", "The challenge creator was not found."));
        }

        creator.CreditBalance(challenge.CreatorPosition.StakeAmountCc);
        challenge.Void(DateTime.UtcNow);
        await userRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result<ChallengeMutationResultDto>.Success(new ChallengeMutationResultDto(ChallengeDto.FromEntity(challenge), creator.CurrentBalanceCc));
    }
}

public sealed class ExpireChallengeHandler
{
    public static Task<Result<ChallengeDto>> Handle(
        ExpireChallengeCommand command,
        IUserRepository userRepository,
        IMatchChallengeRepository challengeRepository,
        IApplicationTransactionFactory transactionFactory,
        CancellationToken cancellationToken)
    {
        return ChallengeLifecycle.RefundAndCloseAsync(command.ChallengeId, MatchChallengeStatus.Expired, userRepository, challengeRepository, transactionFactory, cancellationToken);
    }
}

file static class ChallengeLifecycle
{
    public static async Task<Result<ChallengeDto>> RefundAndCloseAsync(
        int challengeId,
        MatchChallengeStatus terminalStatus,
        IUserRepository userRepository,
        IMatchChallengeRepository challengeRepository,
        IApplicationTransactionFactory transactionFactory,
        CancellationToken cancellationToken)
    {
        await using var transaction = await transactionFactory.BeginSerializableAsync(cancellationToken);

        var challenge = await challengeRepository.GetForUpdateAsync(challengeId, cancellationToken);
        if (challenge is null)
        {
            return Result<ChallengeDto>.Failure(new Error("challenges.not_found", "The challenge was not found."));
        }

        if (!challenge.IsActive)
        {
            return Result<ChallengeDto>.Failure(new Error("challenges.terminal", "Terminal challenges cannot be changed."));
        }

        foreach (var position in challenge.Positions)
        {
            var participant = await userRepository.GetByIdAsync(position.UserId, cancellationToken);
            if (participant is null)
            {
                return Result<ChallengeDto>.Failure(new Error("challenges.participant_not_found", "A challenge participant was not found."));
            }

            participant.CreditBalance(position.StakeAmountCc);
        }

        if (terminalStatus == MatchChallengeStatus.Voided)
        {
            challenge.Void(DateTime.UtcNow);
        }
        else
        {
            challenge.Expire(DateTime.UtcNow);
        }

        await userRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result<ChallengeDto>.Success(ChallengeDto.FromEntity(challenge));
    }
}
