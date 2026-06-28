using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Domain.Notifications;
using MediatR;

namespace CCE.Application.Notifications.Admin.Queries.GetNotificationLogById;

public sealed class GetNotificationLogByIdQueryHandler
    : IRequestHandler<GetNotificationLogByIdQuery, Response<NotificationLogDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetNotificationLogByIdQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<NotificationLogDto>> Handle(
        GetNotificationLogByIdQuery request,
        CancellationToken cancellationToken)
    {
        var log = (await _db.NotificationLogs
            .Where(l => l.Id == request.Id)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false))
            .FirstOrDefault();

        return log is null
            ? _msg.NotFound<NotificationLogDto>(MessageKeys.Notifications.NOTIFICATION_NOT_FOUND)
            : _msg.Ok(MapToDto(log), MessageKeys.General.ITEMS_LISTED);
    }

    internal static NotificationLogDto MapToDto(NotificationLog l) => new(
        l.Id,
        l.RecipientUserId,
        l.TemplateCode,
        l.TemplateId,
        l.Channel,
        l.Status,
        l.ProviderMessageId,
        l.Error,
        l.AttemptCount,
        l.CreatedOn,
        l.SentOn,
        l.FailedOn,
        l.CorrelationId,
        l.PayloadJson);
}
