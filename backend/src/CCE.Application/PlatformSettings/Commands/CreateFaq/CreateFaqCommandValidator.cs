using FluentValidation;

namespace CCE.Application.PlatformSettings.Commands.CreateFaq;

public sealed class CreateFaqCommandValidator
    : AbstractValidator<CreateFaqCommand>
{
    public CreateFaqCommandValidator()
    {
        RuleFor(x => x.QuestionAr).NotEmpty().MaximumLength(500);
        RuleFor(x => x.QuestionEn).NotEmpty().MaximumLength(500);
        RuleFor(x => x.AnswerAr).NotEmpty();
        RuleFor(x => x.AnswerEn).NotEmpty();
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
    }
}
