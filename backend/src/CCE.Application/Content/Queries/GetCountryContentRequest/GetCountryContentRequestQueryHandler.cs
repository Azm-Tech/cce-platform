using CCE.Application.Common;
using CCE.Application.Common.CountryScope;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Application.Messages;

using MediatR;

namespace CCE.Application.Content.Queries.GetCountryContentRequest;

public sealed class GetCountryContentRequestQueryHandler
    : IRequestHandler<GetCountryContentRequestQuery, Response<CountryContentRequestDto>>
{
    private readonly ICceDbContext _db;
    private readonly ICountryScopeAccessor _scope;
    private readonly MessageFactory _messages;

    public GetCountryContentRequestQueryHandler(
        ICceDbContext db,
        ICountryScopeAccessor scope,
        MessageFactory messages)
    {
        _db = db;
        _scope = scope;
        _messages = messages;
    }

    public async Task<Response<CountryContentRequestDto>> Handle(
        GetCountryContentRequestQuery request,
        CancellationToken cancellationToken)
    {
        var authorizedIds = await _scope.GetAuthorizedCountryIdsAsync(cancellationToken).ConfigureAwait(false);

        var items = await _db.CountryContentRequests
            .Where(r => r.Id == request.Id)
            .WhereIf(authorizedIds is not null, r => authorizedIds!.Contains(r.CountryId))
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false);

        var entity = items.FirstOrDefault();
        if (entity is null)
            return _messages.NotFound<CountryContentRequestDto>(MessageKeys.Content.COUNTRY_RESOURCE_REQUEST_NOT_FOUND);

        var dto = new CountryContentRequestDto(
            entity.Id, entity.CountryId, entity.RequestedById, entity.Type, entity.Status,
            entity.ProposedTitleAr, entity.ProposedTitleEn,
            entity.ProposedDescriptionAr, entity.ProposedDescriptionEn,
            entity.ProposedResourceType, entity.ProposedAssetFileId,
            entity.ProposedTopicId, entity.ProposedCategoryId,
            entity.ProposedStartsOn, entity.ProposedEndsOn,
            entity.ProposedLocationAr, entity.ProposedLocationEn, entity.ProposedOnlineMeetingUrl,
            entity.SubmittedOn, entity.AdminNotesAr, entity.AdminNotesEn,
            entity.ProcessedById, entity.ProcessedOn,
            entity.ProposedKnowledgeLevelId, entity.ProposedJobSectorId);

        return _messages.Ok(dto, MessageKeys.General.SUCCESS_OPERATION);
    }
}
