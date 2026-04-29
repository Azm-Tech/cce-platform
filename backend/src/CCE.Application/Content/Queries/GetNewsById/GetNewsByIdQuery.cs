using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Queries.GetNewsById;

public sealed record GetNewsByIdQuery(System.Guid Id) : IRequest<NewsDto?>;
