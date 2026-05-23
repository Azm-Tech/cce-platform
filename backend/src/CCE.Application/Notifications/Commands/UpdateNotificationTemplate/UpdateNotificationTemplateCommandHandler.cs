using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Notifications.Commands.UpdateNotificationTemplate;

public sealed class UpdateNotificationTemplateCommandHandler
    : IRequestHandler<UpdateNotificationTemplateCommand, Response<System.Guid>>
{
    private readonly INotificationTemplateRepository _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public UpdateNotificationTemplateCommandHandler(
        INotificationTemplateRepository repo,
        ICceDbContext db,
        MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _msg = msg;
    }

    public async Task<Response<System.Guid>> Handle(
        UpdateNotificationTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var template = await _repo.GetAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (template is null)
        {
            return _msg.NotificationTemplateNotFound<System.Guid>();
        }

        template.UpdateContent(request.SubjectAr, request.SubjectEn, request.BodyAr, request.BodyEn);

        if (request.IsActive)
            template.Activate();
        else
            template.Deactivate();

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.NotificationTemplateUpdated(template.Id);
    }
}
