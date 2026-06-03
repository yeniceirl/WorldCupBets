namespace WorldCupBets.Application.Features.Admin;

public sealed record CreateUserInvitationDto(
    int Id,
    string Email,
    string RoleName,
    bool WasAlreadyInvited);
