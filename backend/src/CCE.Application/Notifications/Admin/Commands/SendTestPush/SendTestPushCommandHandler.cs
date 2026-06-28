using CCE.Application.Common;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Notifications.Admin.Commands.SendTestPush;

public sealed class SendTestPushCommandHandler
    : IRequestHandler<SendTestPushCommand, Response<TestPushResultDto>>
{
    private readonly IFirebasePushService _push;
    private readonly MessageFactory _msg;

    public SendTestPushCommandHandler(IFirebasePushService push, MessageFactory msg)
    {
        _push = push;
        _msg = msg;
    }

    public async Task<Response<TestPushResultDto>> Handle(
        SendTestPushCommand request, CancellationToken cancellationToken)
    {
        var (sent, failed) = await _push
            .SendAsync(request.Token, request.Title, request.Body, cancellationToken)
            .ConfigureAwait(false);
        return _msg.Ok(new TestPushResultDto(sent, failed), MessageKeys.General.SUCCESS_OPERATION);
    }
}
