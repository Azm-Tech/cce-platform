using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Application.Notifications.Public.Dtos;
using CCE.Domain.Notifications;
using MediatR;

namespace CCE.Application.Notifications.Public.Queries.GetMyNotificationSettings;

public sealed class GetMyNotificationSettingsQueryHandler
    : IRequestHandler<GetMyNotificationSettingsQuery, Response<IReadOnlyCollection<NotificationSettingsDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetMyNotificationSettingsQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<IReadOnlyCollection<NotificationSettingsDto>>> Handle(
        GetMyNotificationSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var explicitSettings = await _db.UserNotificationSettings
            .Where(s => s.UserId == request.UserId)
            .OrderBy(s => s.Channel)
            .ThenBy(s => s.EventCode)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        var dtos = explicitSettings
            .Select(s => new NotificationSettingsDto(s.Channel, s.EventCode, s.IsEnabled))
            .ToList();

        // Ensure every channel has at least a default entry
        foreach (NotificationChannel channel in Enum.GetValues<NotificationChannel>())
        {
            if (!dtos.Any(d => d.Channel == channel && d.EventCode is null))
            {
                dtos.Insert(0, new NotificationSettingsDto(channel, null, true));
            }
        }

        return _msg.Ok<IReadOnlyCollection<NotificationSettingsDto>>(dtos, "ITEMS_LISTED");
    }
}
