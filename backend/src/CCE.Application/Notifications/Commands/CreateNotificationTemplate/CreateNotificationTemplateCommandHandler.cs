using CCE.Application.Notifications.Dtos;
using CCE.Application.Notifications.Queries.ListNotificationTemplates;
using CCE.Domain.Notifications;
using MediatR;

namespace CCE.Application.Notifications.Commands.CreateNotificationTemplate;

public sealed class CreateNotificationTemplateCommandHandler
    : IRequestHandler<CreateNotificationTemplateCommand, NotificationTemplateDto>
{
    private readonly INotificationTemplateService _service;

    public CreateNotificationTemplateCommandHandler(INotificationTemplateService service)
    {
        _service = service;
    }

    public async Task<NotificationTemplateDto> Handle(
        CreateNotificationTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var template = NotificationTemplate.Define(
            request.Code,
            request.SubjectAr,
            request.SubjectEn,
            request.BodyAr,
            request.BodyEn,
            request.Channel,
            request.VariableSchemaJson);

        await _service.SaveAsync(template, cancellationToken).ConfigureAwait(false);

        return ListNotificationTemplatesQueryHandler.MapToDto(template);
    }
}
