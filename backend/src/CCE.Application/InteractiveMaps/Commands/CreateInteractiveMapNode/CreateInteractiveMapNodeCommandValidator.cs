using CCE.Application.Messages;
using FluentValidation;

namespace CCE.Application.InteractiveMaps.Commands.CreateInteractiveMapNode;

internal sealed class CreateInteractiveMapNodeCommandValidator : AbstractValidator<CreateInteractiveMapNodeCommand>
{
    public CreateInteractiveMapNodeCommandValidator()
    {
        RuleFor(x => x.InteractiveMapId)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD);
        RuleFor(x => x.NameAr)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .MaximumLength(256).WithErrorCode(MessageKeys.Validation.MAX_LENGTH);
        RuleFor(x => x.NameEn)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .MaximumLength(256).WithErrorCode(MessageKeys.Validation.MAX_LENGTH);
        RuleFor(x => x.IconKey)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .MaximumLength(128).WithErrorCode(MessageKeys.Validation.MAX_LENGTH);
        RuleFor(x => x.TopicId)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD);

        RuleFor(x => x.TitleAr)
            .MaximumLength(512).WithErrorCode(MessageKeys.Validation.MAX_LENGTH);

        RuleFor(x => x.TitleEn)
            .MaximumLength(512).WithErrorCode(MessageKeys.Validation.MAX_LENGTH);
    }
}
