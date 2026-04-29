using FluentValidation;

namespace CCE.Application.Surveys.Commands.SubmitServiceRating;

public sealed class SubmitServiceRatingCommandValidator : AbstractValidator<SubmitServiceRatingCommand>
{
    public SubmitServiceRatingCommandValidator()
    {
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Page).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Locale).Must(l => l == "ar" || l == "en")
            .WithMessage("locale must be 'ar' or 'en'.");
        RuleFor(x => x.CommentAr).MaximumLength(2000).When(x => x.CommentAr is not null);
        RuleFor(x => x.CommentEn).MaximumLength(2000).When(x => x.CommentEn is not null);
    }
}
