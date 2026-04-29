using FluentValidation;

namespace CCE.Application.Assistant.Commands.AskAssistant;

public sealed class AskAssistantCommandValidator : AbstractValidator<AskAssistantCommand>
{
    public AskAssistantCommandValidator()
    {
        RuleFor(x => x.Question).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Locale).Must(l => l == "ar" || l == "en")
            .WithMessage("locale must be 'ar' or 'en'.");
    }
}
