using CCE.Application.Community.Public.Dtos;
using MediatR;

namespace CCE.Application.Community.Public.Queries.GetMyFollows;

public sealed record GetMyFollowsQuery(System.Guid UserId) : IRequest<MyFollowsDto>;
