using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Notifications.Admin.Commands.RetryNotificationLog;

public sealed record RetryNotificationLogCommand(System.Guid Id) : IRequest<Response<System.Guid>>;
