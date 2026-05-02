using FluentValidation;

namespace CCE.Application.Assistant.Commands.AskAssistant;

public sealed class AskAssistantCommandValidator : AbstractValidator<AskAssistantCommand>
{
    public AskAssistantCommandValidator()
    {
        RuleFor(x => x.Messages).NotEmpty().WithMessage("messages must contain at least one entry.");
        RuleFor(x => x.Messages).Must(m => m.Count <= 50)
            .WithMessage("messages must contain no more than 50 entries.");
        RuleFor(x => x.Messages).Must(m => m.Count == 0 || m[^1].Role == "user")
            .WithMessage("the last message must have role 'user'.");
        RuleForEach(x => x.Messages).ChildRules(child =>
        {
            child.RuleFor(m => m.Role).Must(r => r == "user" || r == "assistant")
                .WithMessage("role must be 'user' or 'assistant'.");
            child.RuleFor(m => m.Content).NotEmpty().MaximumLength(4000);
        });
        RuleFor(x => x.Locale).Must(l => l == "ar" || l == "en")
            .WithMessage("locale must be 'ar' or 'en'.");
    }
}
