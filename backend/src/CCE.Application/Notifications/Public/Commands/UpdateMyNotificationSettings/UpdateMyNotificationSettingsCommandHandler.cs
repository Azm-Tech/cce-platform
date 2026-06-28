using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.Notifications;
using MediatR;

namespace CCE.Application.Notifications.Public.Commands.UpdateMyNotificationSettings;

public sealed class UpdateMyNotificationSettingsCommandHandler
    : IRequestHandler<UpdateMyNotificationSettingsCommand, Response<VoidData>>
{
    private readonly IUserNotificationSettingsRepository _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public UpdateMyNotificationSettingsCommandHandler(
        IUserNotificationSettingsRepository repo,
        ICceDbContext db,
        MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(
        UpdateMyNotificationSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await _repo.GetAsync(
            request.UserId,
            request.Channel,
            request.EventCode,
            cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            existing.Update(request.IsEnabled);
        }
        else
        {
            var settings = UserNotificationSettings.Create(
                request.UserId, request.Channel, request.IsEnabled, request.EventCode);
            await _repo.AddAsync(settings, cancellationToken).ConfigureAwait(false);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.Ok(MessageKeys.Notifications.NOTIFICATION_SETTINGS_UPDATED);
    }
}
