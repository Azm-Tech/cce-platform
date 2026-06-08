using CCE.Application.Common;
using CCE.Application.Community.Public.Dtos;
using MediatR;

namespace CCE.Application.Community.Public.Queries.GetCommunityUserProfile;

public sealed record GetCommunityUserProfileQuery(Guid UserId) : IRequest<Response<CommunityUserProfileDto>>;
