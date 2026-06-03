namespace WorldCupBets.Application.Features.Admin;

public sealed record CreateUserInvitationCommand(
    string Email,
    string RoleName);
