using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Country.Dtos;
using CCE.Domain.Country;
using MediatR;

namespace CCE.Application.Country.Queries.GetCountryProfile;

public sealed class GetCountryProfileQueryHandler : IRequestHandler<GetCountryProfileQuery, CountryProfileDto?>
{
    private readonly ICountryProfileService _service;
    private readonly ICceDbContext _db;

    public GetCountryProfileQueryHandler(ICountryProfileService service, ICceDbContext db)
    {
        _service = service;
        _db = db;
    }

    public async Task<CountryProfileDto?> Handle(GetCountryProfileQuery request, CancellationToken cancellationToken)
    {
        var profile = await _service.FindByCountryIdAsync(request.CountryId, cancellationToken).ConfigureAwait(false);
        if (profile is null)
            return null;

        CountryKapsarcSnapshot? snapshot = null;
        var countries = await _db.Countries
            .Where(c => c.Id == request.CountryId)
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var country = countries.FirstOrDefault();
        if (country?.LatestKapsarcSnapshotId.HasValue == true)
        {
            var snapshots = await _db.CountryKapsarcSnapshots
                .Where(s => s.Id == country.LatestKapsarcSnapshotId.Value)
                .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
            snapshot = snapshots.FirstOrDefault();
        }

        return MapToDto(profile, snapshot);
    }

    internal static CountryProfileDto MapToDto(CountryProfile profile, CountryKapsarcSnapshot? snapshot = null) =>
        new(
            profile.Id,
            profile.CountryId,
            profile.DescriptionAr,
            profile.DescriptionEn,
            profile.KeyInitiativesAr,
            profile.KeyInitiativesEn,
            profile.ContactInfoAr,
            profile.ContactInfoEn,
            profile.Population,
            profile.AreaSqKm,
            profile.GdpPerCapita,
            profile.NationallyDeterminedContributionAssetId,
            snapshot?.Classification,
            snapshot?.PerformanceScore,
            snapshot?.TotalIndex,
            snapshot?.SnapshotTakenOn,
            profile.LastModifiedById ?? profile.CreatedById,
            profile.LastModifiedOn ?? profile.CreatedOn);
}
