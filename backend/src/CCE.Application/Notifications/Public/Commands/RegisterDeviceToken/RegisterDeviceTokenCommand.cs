using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Notifications.Public.Commands.RegisterDeviceToken;

public sealed record RegisterDeviceTokenCommand(
    System.Guid UserId,
    string Token,
    string Platform,
    string DeviceId
) : IRequest<Response<VoidData>>;
