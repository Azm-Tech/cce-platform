using CCE.Application.Common;
using CCE.Application.Common.CountryScope;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Application.Errors;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Content.Queries.ListCountryContentRequests;

public sealed class ListCountryContentRequestsQueryHandler
    : IRequestHandler<ListCountryContentRequestsQuery, Response<PagedResult<CountryContentRequestDto>>>
{
    private readonly ICceDbContext _db;
    private readonly ICountryScopeAccessor _scope;
    private readonly MessageFactory _messages;

    public ListCountryContentRequestsQueryHandler(
        ICceDbContext db,
        ICountryScopeAccessor scope,
        MessageFactory messages)
    {
        _db = db;
        _scope = scope;
        _messages = messages;
    }

    public async Task<Response<PagedResult<CountryContentRequestDto>>> Handle(
        ListCountryContentRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var authorizedIds = await _scope.GetAuthorizedCountryIdsAsync(cancellationToken).ConfigureAwait(false);

        // State rep with no country assignment — return empty page (INF005)
        if (authorizedIds is not null && authorizedIds.Count == 0)
            return _messages.Ok(
                new PagedResult<CountryContentRequestDto>([], request.Page, request.PageSize, 0),
                ApplicationErrors.General.SUCCESS_OPERATION);

        var query = _db.CountryContentRequests
            // Scope filter: null = admin bypass, list = state-rep restricted to own countries
            .WhereIf(authorizedIds is not null, r => authorizedIds!.Contains(r.CountryId))
            // Optional filters usable by both admin (US049) and state rep (US051)
            .WhereIf(request.CountryId.HasValue, r => r.CountryId == request.CountryId!.Value)
            .WhereIf(request.Status.HasValue, r => r.Status == request.Status!.Value)
            .WhereIf(request.Type.HasValue, r => r.Type == request.Type!.Value)
            .OrderByDescending(r => r.SubmittedOn);

        var page = await query
            .ToPagedResultAsync(
                r => new CountryContentRequestDto(
                    r.Id, r.CountryId, r.RequestedById, r.Type, r.Status,
                    r.ProposedTitleAr, r.ProposedTitleEn,
                    r.ProposedDescriptionAr, r.ProposedDescriptionEn,
                    r.ProposedResourceType, r.ProposedAssetFileId,
                    r.ProposedTopicId, r.ProposedStartsOn, r.ProposedEndsOn,
                    r.ProposedLocationAr, r.ProposedLocationEn, r.ProposedOnlineMeetingUrl,
                    r.SubmittedOn, r.AdminNotesAr, r.AdminNotesEn,
                    r.ProcessedById, r.ProcessedOn),
                request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        return _messages.Ok(page, ApplicationErrors.General.SUCCESS_OPERATION);
    }
}
