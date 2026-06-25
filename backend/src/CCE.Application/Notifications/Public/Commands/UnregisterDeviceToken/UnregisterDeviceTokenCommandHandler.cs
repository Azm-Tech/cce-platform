using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Notifications.Public.Commands.UnregisterDeviceToken;

public sealed class UnregisterDeviceTokenCommandHandler
    : IRequestHandler<UnregisterDeviceTokenCommand, Response<VoidData>>
{
    private readonly IUserDeviceTokenRepository _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public UnregisterDeviceTokenCommandHandler(
        IUserDeviceTokenRepository repo,
        ICceDbContext db,
        MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(
        UnregisterDeviceTokenCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await _repo
            .GetByUserAndDeviceAsync(request.UserId, request.DeviceId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null || existing.UserId != request.UserId)
            return _msg.NotFound<VoidData>(MessageKeys.Notifications.DEVICE_TOKEN_NOT_FOUND);

        existing.Deactivate();
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return _msg.Ok(MessageKeys.Notifications.DEVICE_TOKEN_DELETED);
    }
}
