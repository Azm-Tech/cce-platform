using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Notifications;
using MediatR;

namespace CCE.Application.Notifications.Public.Commands.RegisterDeviceToken;

public sealed class RegisterDeviceTokenCommandHandler
    : IRequestHandler<RegisterDeviceTokenCommand, Response<VoidData>>
{
    private readonly IUserDeviceTokenRepository _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;
    private readonly ISystemClock _clock;

    public RegisterDeviceTokenCommandHandler(
        IUserDeviceTokenRepository repo,
        ICceDbContext db,
        MessageFactory msg,
        ISystemClock clock)
    {
        _repo = repo;
        _db = db;
        _msg = msg;
        _clock = clock;
    }

    public async Task<Response<VoidData>> Handle(
        RegisterDeviceTokenCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await _repo
            .GetByUserAndDeviceAsync(request.UserId, request.DeviceId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            existing.Refresh(request.Token, _clock);
        }
        else
        {
            var token = UserDeviceToken.Register(
                request.UserId,
                request.DeviceId,
                request.Token,
                request.Platform,
                _clock);
            await _repo.AddAsync(token, cancellationToken).ConfigureAwait(false);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return _msg.Ok(MessageKeys.Notifications.DEVICE_TOKEN_REGISTERED);
    }
}
