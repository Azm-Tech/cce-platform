using CCE.Application.Community.Public.Dtos;
using MediatR;

namespace CCE.Application.Community.Public.Queries.GetPublicPostById;

public sealed record GetPublicPostByIdQuery(System.Guid Id) : IRequest<PublicPostDto?>;
