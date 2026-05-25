using FluentValidation;

namespace CCE.Application.Media.Commands.DeleteMedia;

public sealed class DeleteMediaCommandValidator
    : AbstractValidator<DeleteMediaCommand>
{
    public DeleteMediaCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
