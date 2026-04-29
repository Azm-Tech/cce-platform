using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Notifications.Dtos;
using CCE.Application.Notifications.Queries.ListNotificationTemplates;
using MediatR;

namespace CCE.Application.Notifications.Queries.GetNotificationTemplateById;

public sealed class GetNotificationTemplateByIdQueryHandler
    : IRequestHandler<GetNotificationTemplateByIdQuery, NotificationTemplateDto?>
{
    private readonly ICceDbContext _db;

    public GetNotificationTemplateByIdQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<NotificationTemplateDto?> Handle(
        GetNotificationTemplateByIdQuery request,
        CancellationToken cancellationToken)
    {
        var list = await _db.NotificationTemplates
            .Where(t => t.Id == request.Id)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var template = list.SingleOrDefault();
        return template is null ? null : ListNotificationTemplatesQueryHandler.MapToDto(template);
    }
}
