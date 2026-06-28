using CCE.Application.Common;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using MediatR;

namespace CCE.Application.Community.Public.Queries.ListMyMentions;

/// <summary>Paged list of where the caller was @mentioned.</summary>
public sealed record ListMyMentionsQuery(int Page, int PageSize)
    : IRequest<Response<PagedResult<MyMentionDto>>>;
