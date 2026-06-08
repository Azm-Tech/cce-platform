using CCE.Application.Common;
using CCE.Application.Content.Public.Dtos;
using MediatR;

namespace CCE.Application.Content.Public.Queries.ListResourceTypes;

public sealed record ListResourceTypesQuery()
    : IRequest<Response<List<ResourceTypeDto>>>;
