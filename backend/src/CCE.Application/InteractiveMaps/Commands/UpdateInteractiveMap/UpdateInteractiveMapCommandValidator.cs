using CCE.Application.Errors;
using FluentValidation;

namespace CCE.Application.InteractiveMaps.Commands.UpdateInteractiveMap;

internal sealed class UpdateInteractiveMapCommandValidator : AbstractValidator<UpdateInteractiveMapCommand>
{
    public UpdateInteractiveMapCommandValidator()
    {
        RuleFor(x => x.NameAr)
            .NotEmpty().WithErrorCode(ApplicationErrors.Validation.REQUIRED_FIELD)
            .MaximumLength(256).WithErrorCode(ApplicationErrors.Validation.MAX_LENGTH);
        RuleFor(x => x.NameEn)
            .NotEmpty().WithErrorCode(ApplicationErrors.Validation.REQUIRED_FIELD)
            .MaximumLength(256).WithErrorCode(ApplicationErrors.Validation.MAX_LENGTH);
        RuleFor(x => x.DescriptionAr)
            .MaximumLength(512).WithErrorCode(ApplicationErrors.Validation.MAX_LENGTH);
        RuleFor(x => x.DescriptionEn)
            .MaximumLength(512).WithErrorCode(ApplicationErrors.Validation.MAX_LENGTH);
    }
}
