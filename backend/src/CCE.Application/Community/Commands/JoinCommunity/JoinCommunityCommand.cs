using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Community.Commands.JoinCommunity;

/// <summary>Join a public community instantly, or request to join a private one.</summary>
public sealed record JoinCommunityCommand(Guid CommunityId) : IRequest<Response<VoidData>>;
