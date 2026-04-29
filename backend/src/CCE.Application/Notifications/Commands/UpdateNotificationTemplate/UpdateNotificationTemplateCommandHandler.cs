using CCE.Application.Notifications.Dtos;
using CCE.Application.Notifications.Queries.ListNotificationTemplates;
using MediatR;

namespace CCE.Application.Notifications.Commands.UpdateNotificationTemplate;

public sealed class UpdateNotificationTemplateCommandHandler
    : IRequestHandler<UpdateNotificationTemplateCommand, NotificationTemplateDto?>
{
    private readonly INotificationTemplateService _service;

    public UpdateNotificationTemplateCommandHandler(INotificationTemplateService service)
    {
        _service = service;
    }

    public async Task<NotificationTemplateDto?> Handle(
        UpdateNotificationTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var template = await _service.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (template is null)
        {
            return null;
        }

        template.UpdateContent(request.SubjectAr, request.SubjectEn, request.BodyAr, request.BodyEn);

        if (request.IsActive)
            template.Activate();
        else
            template.Deactivate();

        await _service.UpdateAsync(template, cancellationToken).ConfigureAwait(false);

        return ListNotificationTemplatesQueryHandler.MapToDto(template);
    }
}
