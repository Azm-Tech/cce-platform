using FluentValidation;

namespace CCE.Application.Identity.Commands.DeleteUser;

public sealed class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
    }
}
