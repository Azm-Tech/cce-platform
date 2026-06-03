using FluentValidation;

namespace CCE.Application.Content.Commands.SubmitCountryNewsRequest;

public sealed class SubmitCountryNewsRequestCommandValidator
    : AbstractValidator<SubmitCountryNewsRequestCommand>
{
    public SubmitCountryNewsRequestCommandValidator()
    {
        RuleFor(x => x.CountryId).NotEmpty();
        RuleFor(x => x.TitleAr).NotEmpty().MaximumLength(512);
        RuleFor(x => x.TitleEn).NotEmpty().MaximumLength(512);
        RuleFor(x => x.ContentAr).NotEmpty();
        RuleFor(x => x.ContentEn).NotEmpty();
        RuleFor(x => x.TopicId).NotEmpty();
    }
}
