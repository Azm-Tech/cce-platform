using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.CountryPublic.Dtos;
using MediatR;

namespace CCE.Application.CountryPublic.Queries.GetPublicCountryProfile;

public sealed class GetPublicCountryProfileQueryHandler
    : IRequestHandler<GetPublicCountryProfileQuery, PublicCountryProfileDto?>
{
    private readonly ICceDbContext _db;

    public GetPublicCountryProfileQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<PublicCountryProfileDto?> Handle(
        GetPublicCountryProfileQuery request,
        CancellationToken cancellationToken)
    {
        // Verify country exists and is active
        var countryQuery = _db.Countries.Where(c => c.Id == request.CountryId && c.IsActive);
        var countryExists = (await countryQuery.ToListAsyncEither(cancellationToken).ConfigureAwait(false)).Count > 0;
        if (!countryExists)
        {
            return null;
        }

        // Load profile by CountryId
        var profileQuery = _db.CountryProfiles.Where(p => p.CountryId == request.CountryId);
        var profiles = await profileQuery.ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var profile = profiles.FirstOrDefault();
        if (profile is null)
        {
            return null;
        }

        return MapToDto(profile);
    }

    internal static PublicCountryProfileDto MapToDto(CCE.Domain.Country.CountryProfile p) => new(
        p.Id,
        p.CountryId,
        p.DescriptionAr,
        p.DescriptionEn,
        p.KeyInitiativesAr,
        p.KeyInitiativesEn,
        p.ContactInfoAr,
        p.ContactInfoEn,
        p.LastUpdatedOn);
}
