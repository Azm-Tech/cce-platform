using CCE.Application.Common;
using CCE.Application.Identity.Public.Dtos;
using MediatR;

namespace CCE.Application.Identity.Public.Queries.GetMyProfile;

public sealed record GetMyProfileQuery(System.Guid UserId) : IRequest<Response<UserProfileDto>>;
