using CCE.Application.Common;
using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Queries.GetResourceById;

public sealed record GetResourceByIdQuery(System.Guid Id) : IRequest<Response<ResourceDto>>;
