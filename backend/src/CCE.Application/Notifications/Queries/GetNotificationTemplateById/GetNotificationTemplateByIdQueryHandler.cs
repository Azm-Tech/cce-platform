using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Application.Notifications.Dtos;
using CCE.Application.Notifications.Queries.ListNotificationTemplates;
using MediatR;

namespace CCE.Application.Notifications.Queries.GetNotificationTemplateById;

public sealed class GetNotificationTemplateByIdQueryHandler
    : IRequestHandler<GetNotificationTemplateByIdQuery, Response<NotificationTemplateDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetNotificationTemplateByIdQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<NotificationTemplateDto>> Handle(
        GetNotificationTemplateByIdQuery request,
        CancellationToken cancellationToken)
    {
        var list = await _db.NotificationTemplates
            .Where(t => t.Id == request.Id)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var template = list.SingleOrDefault();
        return template is null
            ? _msg.NotificationTemplateNotFound<NotificationTemplateDto>()
            : _msg.Ok(ListNotificationTemplatesQueryHandler.MapToDto(template), "ITEMS_LISTED");
    }
}
