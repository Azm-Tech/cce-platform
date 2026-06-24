using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Community.Commands.SoftDeleteReply;

public sealed record SoftDeleteReplyCommand(System.Guid Id) : IRequest<Response<VoidData>>;
