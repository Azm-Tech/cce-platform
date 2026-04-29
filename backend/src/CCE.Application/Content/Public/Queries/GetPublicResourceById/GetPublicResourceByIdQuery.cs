using CCE.Application.Content.Public.Dtos;
using MediatR;

namespace CCE.Application.Content.Public.Queries.GetPublicResourceById;

public sealed record GetPublicResourceByIdQuery(System.Guid Id) : IRequest<PublicResourceDto?>;
