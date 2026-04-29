using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Queries.GetPageById;

public sealed record GetPageByIdQuery(System.Guid Id) : IRequest<PageDto?>;
