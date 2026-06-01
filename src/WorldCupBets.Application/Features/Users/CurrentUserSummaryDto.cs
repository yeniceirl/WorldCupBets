namespace WorldCupBets.Application.Features.Users;

public sealed record CurrentUserSummaryDto(
    int Id,
    string DisplayName,
    string Email,
    int CurrentBalanceCc,
    int RescueCount,
    int RescueDebtCc);
