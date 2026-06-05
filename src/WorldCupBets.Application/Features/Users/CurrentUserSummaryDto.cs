namespace WorldCupBets.Application.Features.Users;

public sealed record CurrentUserSummaryDto(
    int Id,
    string DisplayName,
    string Email,
    decimal CurrentBalanceCc,
    int RescueCount,
    decimal RescueDebtCc);
