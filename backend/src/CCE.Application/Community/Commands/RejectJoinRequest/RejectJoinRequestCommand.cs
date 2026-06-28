using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Community.Commands.RejectJoinRequest;

public sealed record RejectJoinRequestCommand(Guid RequestId) : IRequest<Response<VoidData>>;
