using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Notifications.Public.Commands.MarkNotificationRead;

public sealed record MarkNotificationReadCommand(System.Guid Id, System.Guid UserId) : IRequest<Response<VoidData>>;
