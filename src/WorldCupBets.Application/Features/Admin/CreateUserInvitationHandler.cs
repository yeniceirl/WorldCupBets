using WorldCupBets.Domain.Common;
using WorldCupBets.Domain.Entities;
using WorldCupBets.Domain.Repositories;

namespace WorldCupBets.Application.Features.Admin;

public sealed class CreateUserInvitationHandler
{
    public static async Task<Result<CreateUserInvitationDto>> Handle(
        CreateUserInvitationCommand command,
        IUserInvitationRepository userInvitationRepository,
        IRoleRepository roleRepository,
        CancellationToken cancellationToken)
    {
        var role = await roleRepository.GetByNameAsync(command.RoleName, cancellationToken);
        if (role is null)
        {
            return Result<CreateUserInvitationDto>.Failure(new Error("admin.role_not_found", "The invited role is not configured."));
        }

        var existingInvitation = await userInvitationRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (existingInvitation is not null)
        {
            return Result<CreateUserInvitationDto>.Success(new CreateUserInvitationDto(
                existingInvitation.Id,
                existingInvitation.Email,
                existingInvitation.RoleName,
                WasAlreadyInvited: true));
        }

        var invitation = UserInvitation.Create(command.Email, command.RoleName);
        await userInvitationRepository.AddAsync(invitation, cancellationToken);
        await userInvitationRepository.SaveChangesAsync(cancellationToken);

        return Result<CreateUserInvitationDto>.Success(new CreateUserInvitationDto(
            invitation.Id,
            invitation.Email,
            invitation.RoleName,
            WasAlreadyInvited: false));
    }
}
