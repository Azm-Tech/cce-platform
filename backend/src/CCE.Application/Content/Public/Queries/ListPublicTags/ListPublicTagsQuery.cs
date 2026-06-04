using CCE.Application.Common;
using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Public.Queries.ListPublicTags;

public sealed record ListPublicTagsQuery(string? Search = null) : IRequest<Response<System.Collections.Generic.List<TagDto>>>;
