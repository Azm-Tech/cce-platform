using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Community.Commands.FollowCommunity;

public sealed record FollowCommunityCommand(Guid CommunityId) : IRequest<Response<VoidData>>;
