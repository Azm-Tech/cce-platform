using MediatR;

namespace CCE.Application.Notifications.Public.Queries.GetMyUnreadCount;

public sealed record GetMyUnreadCountQuery(System.Guid UserId) : IRequest<int>;
