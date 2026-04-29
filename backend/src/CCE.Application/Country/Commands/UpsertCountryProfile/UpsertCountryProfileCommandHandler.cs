using CCE.Application.Common.Interfaces;
using CCE.Application.Country.Dtos;
using CCE.Application.Country.Queries.GetCountryProfile;
using CCE.Domain.Common;
using CCE.Domain.Country;
using MediatR;

namespace CCE.Application.Country.Commands.UpsertCountryProfile;

public sealed class UpsertCountryProfileCommandHandler : IRequestHandler<UpsertCountryProfileCommand, CountryProfileDto>
{
    private readonly ICountryProfileService _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;

    public UpsertCountryProfileCommandHandler(
        ICountryProfileService service,
        ICurrentUserAccessor currentUser,
        ISystemClock clock)
    {
        _service = service;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<CountryProfileDto> Handle(UpsertCountryProfileCommand request, CancellationToken cancellationToken)
    {
        var adminId = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot upsert country profile from a request without a user identity.");

        var existing = await _service.FindByCountryIdAsync(request.CountryId, cancellationToken).ConfigureAwait(false);

        CountryProfile result;
        if (existing is null)
        {
            result = CountryProfile.Create(
                request.CountryId,
                request.DescriptionAr,
                request.DescriptionEn,
                request.KeyInitiativesAr,
                request.KeyInitiativesEn,
                request.ContactInfoAr,
                request.ContactInfoEn,
                adminId,
                _clock);
            await _service.SaveAsync(result, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            existing.Update(
                request.DescriptionAr,
                request.DescriptionEn,
                request.KeyInitiativesAr,
                request.KeyInitiativesEn,
                request.ContactInfoAr,
                request.ContactInfoEn,
                adminId,
                _clock);
            await _service.UpdateAsync(existing, request.RowVersion, cancellationToken).ConfigureAwait(false);
            result = existing;
        }

        return GetCountryProfileQueryHandler.MapToDto(result);
    }
}
