using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Notifications.Public.Commands.UnregisterDeviceToken;

public sealed record UnregisterDeviceTokenCommand(
    System.Guid UserId,
    string DeviceId
) : IRequest<Response<VoidData>>;
