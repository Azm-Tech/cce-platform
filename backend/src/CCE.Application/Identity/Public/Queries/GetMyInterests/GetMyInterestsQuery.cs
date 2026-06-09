using CCE.Application.Common;
using CCE.Application.Identity.Public.Dtos;
using MediatR;

namespace CCE.Application.Identity.Public.Queries.GetMyInterests;

public sealed record GetMyInterestsQuery(System.Guid UserId) : IRequest<Response<UserInterestsDto>>;