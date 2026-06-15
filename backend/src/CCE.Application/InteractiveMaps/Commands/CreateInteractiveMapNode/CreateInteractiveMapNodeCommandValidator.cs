using CCE.Application.Errors;
using FluentValidation;

namespace CCE.Application.InteractiveMaps.Commands.CreateInteractiveMapNode;

internal sealed class CreateInteractiveMapNodeCommandValidator : AbstractValidator<CreateInteractiveMapNodeCommand>
{
    public CreateInteractiveMapNodeCommandValidator()
    {
        RuleFor(x => x.InteractiveMapId)
            .NotEmpty().WithErrorCode(ApplicationErrors.Validation.REQUIRED_FIELD);
        RuleFor(x => x.NameAr)
            .NotEmpty().WithErrorCode(ApplicationErrors.Validation.REQUIRED_FIELD)
            .MaximumLength(256).WithErrorCode(ApplicationErrors.Validation.MAX_LENGTH);
        RuleFor(x => x.NameEn)
            .NotEmpty().WithErrorCode(ApplicationErrors.Validation.REQUIRED_FIELD)
            .MaximumLength(256).WithErrorCode(ApplicationErrors.Validation.MAX_LENGTH);
        RuleFor(x => x.IconKey)
            .NotEmpty().WithErrorCode(ApplicationErrors.Validation.REQUIRED_FIELD)
            .MaximumLength(128).WithErrorCode(ApplicationErrors.Validation.MAX_LENGTH);
        RuleFor(x => x.Level)
            .GreaterThanOrEqualTo(0).WithErrorCode(ApplicationErrors.Validation.INVALID_FORMAT);
    }
}
