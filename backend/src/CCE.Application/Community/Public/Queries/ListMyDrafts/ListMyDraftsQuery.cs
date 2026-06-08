using CCE.Application.Common;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using MediatR;

namespace CCE.Application.Community.Public.Queries.ListMyDrafts;

/// <summary>Lists the caller's own unpublished drafts.</summary>
public sealed record ListMyDraftsQuery(int Page, int PageSize)
    : IRequest<Response<PagedResult<MyDraftDto>>>;
