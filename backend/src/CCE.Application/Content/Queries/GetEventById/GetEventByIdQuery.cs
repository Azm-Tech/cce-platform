using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Queries.GetEventById;

public sealed record GetEventByIdQuery(System.Guid Id) : IRequest<EventDto?>;
