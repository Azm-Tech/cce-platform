using CCE.Application.Common;
using CCE.Application.Community.Public.Dtos;
using MediatR;

namespace CCE.Application.Community.Public.Queries.GetPublicPostById;

public sealed record GetPublicPostByIdQuery(System.Guid Id, System.Guid? UserId)
    : IRequest<Response<PostDetailDto>>;
