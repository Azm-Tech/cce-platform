using CCE.Application.Messages;
using FluentValidation;

namespace CCE.Application.InteractiveMaps.Commands.UpdateInteractiveMapNode;

internal sealed class UpdateInteractiveMapNodeCommandValidator : AbstractValidator<UpdateInteractiveMapNodeCommand>
{
    public UpdateInteractiveMapNodeCommandValidator()
    {
        RuleFor(x => x.NameAr)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .MaximumLength(256).WithErrorCode(MessageKeys.Validation.MAX_LENGTH);
        RuleFor(x => x.NameEn)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .MaximumLength(256).WithErrorCode(MessageKeys.Validation.MAX_LENGTH);
        RuleFor(x => x.IconKey)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .MaximumLength(128).WithErrorCode(MessageKeys.Validation.MAX_LENGTH);
        RuleFor(x => x.Level)
            .GreaterThanOrEqualTo(0).WithErrorCode(MessageKeys.Validation.INVALID_FORMAT);
        RuleFor(x => x.TopicId)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD);
    }
}
