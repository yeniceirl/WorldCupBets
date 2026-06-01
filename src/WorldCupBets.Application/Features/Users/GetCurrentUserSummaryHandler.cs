using WorldCupBets.Domain.Common;
using WorldCupBets.Domain.Repositories;

namespace WorldCupBets.Application.Features.Users;

public sealed class GetCurrentUserSummaryHandler
{
    public static async Task<Result<CurrentUserSummaryDto>> Handle(
        GetCurrentUserSummaryQuery query,
        IUserRepository userRepository,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(query.UserId, cancellationToken);
        if (user is null)
        {
            return Result<CurrentUserSummaryDto>.Failure(new Error("users.user_not_found", "The authenticated user was not found."));
        }

        if (user.ApplyDeadRescueIfEligible())
        {
            await userRepository.SaveChangesAsync(cancellationToken);
        }

        return Result<CurrentUserSummaryDto>.Success(new CurrentUserSummaryDto(
            user.Id,
            user.DisplayName,
            user.Email,
            user.CurrentBalanceCc,
            user.RescueCount,
            user.RescueDebtCc));
    }
}
