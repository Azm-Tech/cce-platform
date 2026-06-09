using CCE.Application.Common;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Commands.ChangeCommunityVisibility;

public sealed record ChangeCommunityVisibilityCommand(Guid CommunityId, CommunityVisibility Visibility)
    : IRequest<Response<VoidData>>;

public sealed record ChangeCommunityVisibilityRequest(CommunityVisibility Visibility);
