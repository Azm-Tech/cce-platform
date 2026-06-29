using CCE.Application.Common;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using MediatR;

namespace CCE.Application.Community.Public.Queries.ListMyReplies;

public sealed record ListMyRepliesQuery(int Page, int PageSize)
    : IRequest<Response<PagedResult<MyReplyItemDto>>>;
