using CCE.Application.Common;
using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Queries.Tags.GetTagById;

public sealed record GetTagByIdQuery(System.Guid Id) : IRequest<Response<TagDto>>;
