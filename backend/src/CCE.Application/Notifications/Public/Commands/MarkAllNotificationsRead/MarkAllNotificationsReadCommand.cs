using MediatR;

namespace CCE.Application.Notifications.Public.Commands.MarkAllNotificationsRead;

public sealed record MarkAllNotificationsReadCommand(System.Guid UserId) : IRequest<int>;
