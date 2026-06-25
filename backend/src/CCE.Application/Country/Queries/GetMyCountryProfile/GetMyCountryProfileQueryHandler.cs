using CCE.Application.Common;
using CCE.Application.Common.CountryScope;
using CCE.Application.Common.Pagination;
using CCE.Application.Country.Dtos;
using CCE.Application.Country.Queries.GetCountryProfile;
using CCE.Application.Messages;
using CCE.Domain.Country;
using CCE.Application.Common.Interfaces;
using MediatR;

namespace CCE.Application.Country.Queries.GetMyCountryProfile;

public sealed class GetMyCountryProfileQueryHandler : IRequestHandler<GetMyCountryProfileQuery, Response<CountryProfileDto>>
{
    private readonly ICceDbContext _db;
    private readonly ICountryScopeAccessor _scope;
    private readonly MessageFactory _messages;

    public GetMyCountryProfileQueryHandler(
        ICceDbContext db,
        ICountryScopeAccessor scope,
        MessageFactory messages)
    {
        _db = db;
        _scope = scope;
        _messages = messages;
    }

    public async Task<Response<CountryProfileDto>> Handle(
        GetMyCountryProfileQuery request,
        CancellationToken cancellationToken)
    {
        var authorizedIds = await _scope.GetAuthorizedCountryIdsAsync(cancellationToken).ConfigureAwait(false);
        // null = admin (no scope restriction) — this endpoint is state-rep only; empty = no assignment
        if (authorizedIds is not null && authorizedIds.Count == 0)
            return _messages.NotFound<CountryProfileDto>(MessageKeys.Country.NO_COUNTRY_ASSIGNED);

        // Use first assigned country (state reps typically have one)
        var countryId = authorizedIds is { Count: > 0 } ? authorizedIds[0] : System.Guid.Empty;
        if (countryId == System.Guid.Empty)
            return _messages.NotFound<CountryProfileDto>(MessageKeys.Country.COUNTRY_PROFILE_NOT_FOUND);

        var profiles = await _db.CountryProfiles
            .Where(p => p.CountryId == countryId)
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var profile = profiles.FirstOrDefault();
        if (profile is null)
            return _messages.NotFound<CountryProfileDto>(MessageKeys.Country.COUNTRY_PROFILE_NOT_FOUND);

        CountryKapsarcSnapshot? snapshot = null;
        var countries = await _db.Countries
            .Where(c => c.Id == countryId)
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var country = countries.FirstOrDefault();
        if (country?.LatestKapsarcSnapshotId.HasValue == true)
        {
            var snapshots = await _db.CountryKapsarcSnapshots
                .Where(s => s.Id == country.LatestKapsarcSnapshotId.Value)
                .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
            snapshot = snapshots.FirstOrDefault();
        }

        return _messages.Ok(
            GetCountryProfileQueryHandler.MapToDto(profile, snapshot),
            MessageKeys.General.SUCCESS_OPERATION);
    }
}
