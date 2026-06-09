using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Community.Commands.UnfollowCommunity;

public sealed record UnfollowCommunityCommand(Guid CommunityId) : IRequest<Response<VoidData>>;
