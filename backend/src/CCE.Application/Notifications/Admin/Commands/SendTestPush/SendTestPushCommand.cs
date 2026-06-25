using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Notifications.Admin.Commands.SendTestPush;

public sealed record SendTestPushCommand(string Token, string Title, string Body)
    : IRequest<Response<TestPushResultDto>>;

public sealed record TestPushResultDto(int Sent, int Failed);
