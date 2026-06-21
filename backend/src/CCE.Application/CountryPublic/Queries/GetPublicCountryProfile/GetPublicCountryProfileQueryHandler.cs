using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.CountryPublic.Dtos;
using CCE.Application.Errors;
using CCE.Application.Messages;
using CCE.Domain.Content;
using CCE.Domain.Country;
using MediatR;

namespace CCE.Application.CountryPublic.Queries.GetPublicCountryProfile;

public sealed class GetPublicCountryProfileQueryHandler
    : IRequestHandler<GetPublicCountryProfileQuery, Response<PublicCountryProfileDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;

    public GetPublicCountryProfileQueryHandler(ICceDbContext db, MessageFactory messages)
    {
        _db = db;
        _messages = messages;
    }

    public async Task<Response<PublicCountryProfileDto>> Handle(
        GetPublicCountryProfileQuery request,
        CancellationToken cancellationToken)
    {
        // Country must exist — the only hard 404 (ALT001)
        var countries = await _db.Countries
            .Where(c => c.Id == request.CountryId && c.IsActive)
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var country = countries.FirstOrDefault();
        if (country is null)
            return _messages.CountryNotFound<PublicCountryProfileDto>();

        // Editorial profile is optional — return country + KAPSARC data even when absent
        var profiles = await _db.CountryProfiles
            .Where(p => p.CountryId == request.CountryId)
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var profile = profiles.FirstOrDefault();

        // Resolve KAPSARC snapshot via pointer — avoids ORDER BY scan on the time-series table
        CountryKapsarcSnapshot? snapshot = null;
        if (country.LatestKapsarcSnapshotId.HasValue)
        {
            var snapshots = await _db.CountryKapsarcSnapshots
                .Where(s => s.Id == country.LatestKapsarcSnapshotId.Value)
                .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
            snapshot = snapshots.FirstOrDefault();
        }

        // Resolve NDC document info — only when profile and asset exist and are clean
        NdcDocumentDto? ndcDocument = null;
        if (profile?.NationallyDeterminedContributionAssetId.HasValue == true)
        {
            var assets = await _db.AssetFiles
                .Where(a => a.Id == profile.NationallyDeterminedContributionAssetId!.Value
                         && a.VirusScanStatus == VirusScanStatus.Clean)
                .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
            var asset = assets.FirstOrDefault();
            if (asset is not null)
                ndcDocument = new NdcDocumentDto(asset.Id, asset.OriginalFileName);
        }

        return _messages.Ok(MapToDto(country, profile, snapshot, ndcDocument), ApplicationErrors.General.SUCCESS_OPERATION);
    }

    internal static PublicCountryProfileDto MapToDto(
        CCE.Domain.Country.Country country,
        CountryProfile? profile,
        CountryKapsarcSnapshot? snapshot,
        NdcDocumentDto? ndcDocument) => new(
            country.Id,
            country.IsoAlpha3!,
            country.NameAr,
            country.NameEn,
            country.FlagUrl,
            profile?.DescriptionAr,
            profile?.DescriptionEn,
            profile?.KeyInitiativesAr,
            profile?.KeyInitiativesEn,
            profile?.ContactInfoAr,
            profile?.ContactInfoEn,
            profile?.Population,
            profile?.AreaSqKm,
            profile?.GdpPerCapita,
            ndcDocument,
            snapshot?.Classification,
            snapshot?.PerformanceScore,
            snapshot?.TotalIndex,
            snapshot?.SnapshotTakenOn,
            profile?.LastModifiedOn ?? profile?.CreatedOn);
}
