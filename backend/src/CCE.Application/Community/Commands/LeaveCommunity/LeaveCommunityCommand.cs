using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Community.Commands.LeaveCommunity;

public sealed record LeaveCommunityCommand(Guid CommunityId) : IRequest<Response<VoidData>>;
