using FluentValidation;

namespace CCE.Application.Content.Commands.SubmitCountryEventRequest;

public sealed class SubmitCountryEventRequestCommandValidator
    : AbstractValidator<SubmitCountryEventRequestCommand>
{
    public SubmitCountryEventRequestCommandValidator()
    {
        RuleFor(x => x.CountryId).NotEmpty();
        RuleFor(x => x.TitleAr).NotEmpty().MaximumLength(512);
        RuleFor(x => x.TitleEn).NotEmpty().MaximumLength(512);
        RuleFor(x => x.DescriptionAr).NotEmpty();
        RuleFor(x => x.DescriptionEn).NotEmpty();
        RuleFor(x => x.TopicId).NotEmpty();
        RuleFor(x => x.StartsOn).NotEmpty();
        RuleFor(x => x.EndsOn).NotEmpty().GreaterThan(x => x.StartsOn)
            .WithMessage("EndsOn must be after StartsOn.");
    }
}
