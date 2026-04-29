using FluentValidation;

namespace CCE.Application.Notifications.Commands.UpdateNotificationTemplate;

public sealed class UpdateNotificationTemplateCommandValidator
    : AbstractValidator<UpdateNotificationTemplateCommand>
{
    public UpdateNotificationTemplateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.SubjectAr).NotEmpty();
        RuleFor(x => x.SubjectEn).NotEmpty();
        RuleFor(x => x.BodyAr).NotEmpty();
        RuleFor(x => x.BodyEn).NotEmpty();
    }
}
