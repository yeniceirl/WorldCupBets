using FluentValidation;

namespace WorldCupBets.Application.Features.Auth;

public sealed class GoogleLoginCommandValidator : AbstractValidator<GoogleLoginCommand>
{
    public GoogleLoginCommandValidator()
    {
        RuleFor(command => command.IdToken)
            .NotEmpty()
            .WithMessage("Google ID token is required.");
    }
}
