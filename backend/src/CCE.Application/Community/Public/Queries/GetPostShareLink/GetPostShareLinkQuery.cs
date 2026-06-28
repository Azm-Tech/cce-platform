using CCE.Application.Common;
using CCE.Application.Community.Public.Dtos;
using MediatR;

namespace CCE.Application.Community.Public.Queries.GetPostShareLink;

public sealed record GetPostShareLinkQuery(Guid PostId) : IRequest<Response<PostShareLinkDto>>;
