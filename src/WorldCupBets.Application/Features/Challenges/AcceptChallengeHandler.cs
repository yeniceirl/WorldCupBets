using WorldCupBets.Application.Abstractions;
using WorldCupBets.Domain.Common;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;

namespace WorldCupBets.Application.Features.Challenges;

public sealed class AcceptChallengeHandler
{
    public static async Task<Result<ChallengeMutationResultDto>> Handle(
        AcceptChallengeCommand command,
        IUserRepository userRepository,
        IMatchRepository matchRepository,
        IMatchChallengeRepository challengeRepository,
        IApplicationTransactionFactory transactionFactory,
        CancellationToken cancellationToken)
    {
        await using var transaction = await transactionFactory.BeginSerializableAsync(cancellationToken);

        var user = await userRepository.GetByIdAsync(command.TakerUserId, cancellationToken);
        if (user is null)
        {
            return Result<ChallengeMutationResultDto>.Failure(new Error("challenges.user_not_found", "The challenge taker was not found."));
        }

        var challenge = await challengeRepository.GetForUpdateAsync(command.ChallengeId, cancellationToken);
        if (challenge is null)
        {
            return Result<ChallengeMutationResultDto>.Failure(new Error("challenges.not_found", "The challenge was not found."));
        }

        if (challenge.CreatorPosition.UserId == command.TakerUserId)
        {
            return Result<ChallengeMutationResultDto>.Failure(new Error("challenges.self_accept", "The creator cannot accept their own challenge."));
        }

        if (!challenge.IsActive || challenge.Status != Domain.Entities.MatchChallengeStatus.Open)
        {
            return Result<ChallengeMutationResultDto>.Failure(new Error("challenges.not_open", "Only open challenges can be accepted."));
        }

        var match = await matchRepository.GetByIdAsync(challenge.MatchId, cancellationToken);
        if (match is null)
        {
            return Result<ChallengeMutationResultDto>.Failure(new Error("challenges.match_not_found", "The challenge match was not found."));
        }

        if (!match.IsBettingOpenAt(DateTime.UtcNow))
        {
            return Result<ChallengeMutationResultDto>.Failure(new Error("challenges.window_closed", "Challenges can only be accepted while the match betting window is open."));
        }

        if (!user.CanAfford(challenge.StakeAmountCc))
        {
            return Result<ChallengeMutationResultDto>.Failure(new Error("challenges.insufficient_balance", "You do not have enough CopaCoin to accept this challenge."));
        }

        user.DeductBalance(challenge.StakeAmountCc);
        user.ApplyDeadRescueIfEligible();
        challenge.Accept(command.TakerUserId, DateTime.UtcNow);
        await userRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result<ChallengeMutationResultDto>.Success(new ChallengeMutationResultDto(ChallengeDto.FromEntity(challenge), user.CurrentBalanceCc));
    }
}
