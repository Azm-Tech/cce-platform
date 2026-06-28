using CCE.Application.Common;
using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Queries.Tags.ListTags;

public sealed record ListTagsQuery : IRequest<Response<System.Collections.Generic.List<TagDto>>>;
