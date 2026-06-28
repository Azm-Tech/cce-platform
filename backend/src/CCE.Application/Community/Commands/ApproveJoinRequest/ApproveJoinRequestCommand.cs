using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Community.Commands.ApproveJoinRequest;

public sealed record ApproveJoinRequestCommand(Guid RequestId) : IRequest<Response<VoidData>>;
