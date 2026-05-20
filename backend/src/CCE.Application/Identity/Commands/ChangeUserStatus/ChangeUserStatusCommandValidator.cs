using FluentValidation;

namespace CCE.Application.Identity.Commands.ChangeUserStatus;

public sealed class ChangeUserStatusCommandValidator : AbstractValidator<ChangeUserStatusCommand>
{
    public ChangeUserStatusCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
    }
}
