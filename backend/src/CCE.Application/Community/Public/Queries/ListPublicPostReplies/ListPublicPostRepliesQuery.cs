using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using MediatR;

namespace CCE.Application.Community.Public.Queries.ListPublicPostReplies;

public sealed record ListPublicPostRepliesQuery(
    System.Guid PostId,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<PublicPostReplyDto>>;
