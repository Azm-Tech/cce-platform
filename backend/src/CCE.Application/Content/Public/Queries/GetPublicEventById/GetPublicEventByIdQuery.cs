using CCE.Application.Content.Public.Dtos;
using MediatR;

namespace CCE.Application.Content.Public.Queries.GetPublicEventById;

public sealed record GetPublicEventByIdQuery(System.Guid Id) : IRequest<PublicEventDto?>;
