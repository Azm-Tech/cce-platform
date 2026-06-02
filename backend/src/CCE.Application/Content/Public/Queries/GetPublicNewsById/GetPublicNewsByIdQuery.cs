using CCE.Application.Common;
using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Public.Queries.GetPublicNewsById;

public sealed record GetPublicNewsByIdQuery(System.Guid Id) : IRequest<Response<NewsDto>>;
