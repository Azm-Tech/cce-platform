using CCE.Application.Country.Dtos;
using CCE.Domain.Country;
using MediatR;

namespace CCE.Application.Country.Queries.GetCountryProfile;

public sealed class GetCountryProfileQueryHandler : IRequestHandler<GetCountryProfileQuery, CountryProfileDto?>
{
    private readonly ICountryProfileService _service;

    public GetCountryProfileQueryHandler(ICountryProfileService service)
    {
        _service = service;
    }

    public async Task<CountryProfileDto?> Handle(GetCountryProfileQuery request, CancellationToken cancellationToken)
    {
        var profile = await _service.FindByCountryIdAsync(request.CountryId, cancellationToken).ConfigureAwait(false);
        return profile is null ? null : MapToDto(profile);
    }

    internal static CountryProfileDto MapToDto(CountryProfile profile) =>
        new(
            profile.Id,
            profile.CountryId,
            profile.DescriptionAr,
            profile.DescriptionEn,
            profile.KeyInitiativesAr,
            profile.KeyInitiativesEn,
            profile.ContactInfoAr,
            profile.ContactInfoEn,
            profile.LastModifiedById ?? profile.CreatedById,
            profile.LastModifiedOn ?? profile.CreatedOn,
            System.Convert.ToBase64String(profile.RowVersion));
}
