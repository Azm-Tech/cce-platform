using FluentValidation;

namespace CCE.Application.PlatformSettings.Commands.UpdateFaq;

public sealed class UpdateFaqCommandValidator
    : AbstractValidator<UpdateFaqCommand>
{
    public UpdateFaqCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.QuestionAr).NotEmpty().MaximumLength(500);
        RuleFor(x => x.QuestionEn).NotEmpty().MaximumLength(500);
        RuleFor(x => x.AnswerAr).NotEmpty();
        RuleFor(x => x.AnswerEn).NotEmpty();
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
    }
}
