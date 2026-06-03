using FluentValidation;

namespace WorldCupBets.Application.Features.Admin;

public sealed class CreateUserInvitationCommandValidator : AbstractValidator<CreateUserInvitationCommand>
{
    public CreateUserInvitationCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(command => command.RoleName)
            .NotEmpty()
            .Must(roleName => roleName is "Bettor" or "Admin")
            .WithMessage("RoleName must be Bettor or Admin.");
    }
}
