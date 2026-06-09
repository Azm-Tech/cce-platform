using CCE.Application.Common;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using MediatR;

namespace CCE.Application.Community.Public.Queries.GetReplyThread;

/// <summary>Loads the descendant subtree of a reply via its materialized thread path.</summary>
public sealed record GetReplyThreadQuery(Guid ReplyId, int Page, int PageSize)
    : IRequest<Response<PagedResult<PublicPostReplyDto>>>;
