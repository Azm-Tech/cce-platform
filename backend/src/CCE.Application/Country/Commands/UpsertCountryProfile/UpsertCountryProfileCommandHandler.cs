using CCE.Application.Common;
using CCE.Application.Common.CountryScope;
using CCE.Application.Common.Interfaces;
using CCE.Application.Country.Dtos;
using CCE.Application.Country.Queries.GetCountryProfile;
using CCE.Application.Errors;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Country;
using MediatR;

namespace CCE.Application.Country.Commands.UpsertCountryProfile;

public sealed class UpsertCountryProfileCommandHandler : IRequestHandler<UpsertCountryProfileCommand, Response<CountryProfileDto>>
{
    private readonly ICountryProfileService _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ICountryScopeAccessor _scope;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _messages;

    public UpsertCountryProfileCommandHandler(
        ICountryProfileService service,
        ICurrentUserAccessor currentUser,
        ICountryScopeAccessor scope,
        ISystemClock clock,
        MessageFactory messages)
    {
        _service = service;
        _currentUser = currentUser;
        _scope = scope;
        _clock = clock;
        _messages = messages;
    }

    public async Task<Response<CountryProfileDto>> Handle(UpsertCountryProfileCommand request, CancellationToken cancellationToken)
    {
        // State reps may only edit their own assigned country; null scope = admin bypass
        var authorizedIds = await _scope.GetAuthorizedCountryIdsAsync(cancellationToken).ConfigureAwait(false);
        if (authorizedIds is not null && !authorizedIds.Contains(request.CountryId))
            return _messages.CountryScopeForbidden<CountryProfileDto>();

        var userId = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot upsert country profile from a request without a user identity.");

        var existing = await _service.FindByCountryIdAsync(request.CountryId, cancellationToken).ConfigureAwait(false);

        CountryProfile result;
        if (existing is null)
        {
            result = CountryProfile.Create(
                request.CountryId,
                request.DescriptionAr, request.DescriptionEn,
                request.KeyInitiativesAr, request.KeyInitiativesEn,
                request.ContactInfoAr, request.ContactInfoEn,
                userId, _clock,
                population: request.Population,
                areaSqKm: request.AreaSqKm,
                gdpPerCapita: request.GdpPerCapita,
                nationallyDeterminedContributionAssetId: request.NdcAssetId);
            await _service.SaveAsync(result, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            existing.Update(
                request.DescriptionAr, request.DescriptionEn,
                request.KeyInitiativesAr, request.KeyInitiativesEn,
                request.ContactInfoAr, request.ContactInfoEn,
                userId, _clock,
                population: request.Population,
                areaSqKm: request.AreaSqKm,
                gdpPerCapita: request.GdpPerCapita,
                nationallyDeterminedContributionAssetId: request.NdcAssetId);
            await _service.UpdateAsync(existing, existing.RowVersion, cancellationToken).ConfigureAwait(false);
            result = existing;
        }

        return _messages.Ok(GetCountryProfileQueryHandler.MapToDto(result), ApplicationErrors.Country.COUNTRY_PROFILE_UPDATED);
    }
}
