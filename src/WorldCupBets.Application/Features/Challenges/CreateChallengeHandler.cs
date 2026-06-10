using WorldCupBets.Application.Abstractions;
using WorldCupBets.Domain.Common;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;

namespace WorldCupBets.Application.Features.Challenges;

public sealed class CreateChallengeHandler
{
    private const string CreatorSideText = "Claim happens";
    private const string TakerSideText = "Claim does not happen";

    public static async Task<Result<ChallengeMutationResultDto>> Handle(
        CreateChallengeCommand command,
        IUserRepository userRepository,
        IMatchRepository matchRepository,
        IMatchChallengeRepository challengeRepository,
        IApplicationTransactionFactory transactionFactory,
        CancellationToken cancellationToken)
    {
        await using var transaction = await transactionFactory.BeginSerializableAsync(cancellationToken);

        var user = await userRepository.GetByIdAsync(command.CreatorUserId, cancellationToken);
        if (user is null)
        {
            return Result<ChallengeMutationResultDto>.Failure(new Error("challenges.user_not_found", "The challenge creator was not found."));
        }

        var match = await matchRepository.GetByIdAsync(command.MatchId, cancellationToken);
        if (match is null)
        {
            return Result<ChallengeMutationResultDto>.Failure(new Error("challenges.match_not_found", "The selected match was not found."));
        }

        if (!match.IsBettingOpenAt(DateTime.UtcNow))
        {
            return Result<ChallengeMutationResultDto>.Failure(new Error("challenges.window_closed", "Challenges can only be created while the match betting window is open."));
        }

        MatchChallenge challenge;
        try
        {
            challenge = MatchChallenge.Create(
                command.CreatorUserId,
                command.MatchId,
                command.ClaimText,
                CreatorSideText,
                TakerSideText,
                command.StakeAmountCc,
                DateTime.UtcNow);
        }
        catch (ArgumentException exception)
        {
            return Result<ChallengeMutationResultDto>.Failure(new Error("challenges.invalid_payload", exception.Message));
        }

        if (!user.CanAfford(challenge.StakeAmountCc))
        {
            return Result<ChallengeMutationResultDto>.Failure(new Error("challenges.insufficient_balance", "You do not have enough CopaCoin to create this challenge."));
        }

        user.DeductBalance(challenge.StakeAmountCc);
        user.ApplyDeadRescueIfEligible();
        await challengeRepository.AddAsync(challenge, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result<ChallengeMutationResultDto>.Success(new ChallengeMutationResultDto(ChallengeDto.FromEntity(challenge, match), user.CurrentBalanceCc));
    }
}
