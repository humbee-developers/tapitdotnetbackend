using FluentValidation;

namespace TapitAI.Application.Features.Auth.Commands;

public class AdminLoginCommandValidator : AbstractValidator<AdminLoginCommand>
{
    public AdminLoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty().MinimumLength(6);
    }
}
