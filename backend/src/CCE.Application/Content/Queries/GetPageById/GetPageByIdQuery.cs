using CCE.Application.Common;
using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Queries.GetPageById;

public sealed record GetPageByIdQuery(System.Guid Id) : IRequest<Response<PageDto>>;
