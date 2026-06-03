using FluentValidation;

namespace CCE.Application.Content.Commands.SubmitCountryResourceRequest;

public sealed class SubmitCountryResourceRequestCommandValidator
    : AbstractValidator<SubmitCountryResourceRequestCommand>
{
    public SubmitCountryResourceRequestCommandValidator()
    {
        RuleFor(x => x.CountryId).NotEmpty();
        RuleFor(x => x.TitleAr).NotEmpty().MaximumLength(512);
        RuleFor(x => x.TitleEn).NotEmpty().MaximumLength(512);
        RuleFor(x => x.DescriptionAr).NotEmpty();
        RuleFor(x => x.DescriptionEn).NotEmpty();
        RuleFor(x => x.AssetFileId).NotEmpty();
    }
}
