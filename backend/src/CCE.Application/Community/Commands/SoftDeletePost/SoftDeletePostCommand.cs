using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Community.Commands.SoftDeletePost;

public sealed record SoftDeletePostCommand(System.Guid Id) : IRequest<Response<VoidData>>;
