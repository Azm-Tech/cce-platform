using FluentValidation;

namespace CCE.Application.Notifications.Commands.CreateNotificationTemplate;

public sealed class CreateNotificationTemplateCommandValidator
    : AbstractValidator<CreateNotificationTemplateCommand>
{
    public CreateNotificationTemplateCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Matches(@"^[A-Z][A-Z0-9_]+$")
            .WithMessage("Code must match ^[A-Z][A-Z0-9_]+$");
        RuleFor(x => x.SubjectAr).NotEmpty();
        RuleFor(x => x.SubjectEn).NotEmpty();
        RuleFor(x => x.BodyAr).NotEmpty();
        RuleFor(x => x.BodyEn).NotEmpty();
        RuleFor(x => x.VariableSchemaJson).NotEmpty();
    }
}
