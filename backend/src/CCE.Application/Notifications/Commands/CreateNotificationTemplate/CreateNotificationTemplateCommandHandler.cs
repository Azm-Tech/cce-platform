using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.Notifications;
using MediatR;

namespace CCE.Application.Notifications.Commands.CreateNotificationTemplate;

public sealed class CreateNotificationTemplateCommandHandler
    : IRequestHandler<CreateNotificationTemplateCommand, Response<System.Guid>>
{
    private readonly INotificationTemplateRepository _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public CreateNotificationTemplateCommandHandler(
        INotificationTemplateRepository repo,
        ICceDbContext db,
        MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _msg = msg;
    }

    public async Task<Response<System.Guid>> Handle(
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

        await _repo.AddAsync(template, cancellationToken).ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.Ok(template.Id, MessageKeys.Notifications.NOTIFICATION_TEMPLATE_CREATED);
    }
}
