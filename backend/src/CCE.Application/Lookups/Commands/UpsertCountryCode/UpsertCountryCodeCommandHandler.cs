using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Lookups;
using CCE.Domain.PlatformSettings.ValueObjects;
using MediatR;

namespace CCE.Application.Lookups.Commands.UpsertCountryCode;

public sealed class UpsertCountryCodeCommandHandler
    : IRequestHandler<UpsertCountryCodeCommand, Response<CountryCodeDto>>
{
    private readonly IRepository<CountryCode, System.Guid> _repo;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;

    public UpsertCountryCodeCommandHandler(
        IRepository<CountryCode, System.Guid> repo,
        ICceDbContext db,
        ICurrentUserAccessor currentUser,
        ISystemClock clock,
        MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _msg = msg;
    }

    public async Task<Response<CountryCodeDto>> Handle(
        UpsertCountryCodeCommand request,
        CancellationToken cancellationToken)
    {
        var by = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot upsert country code from a request without a user identity.");

        var name = LocalizedText.Create(request.NameAr, request.NameEn);

        if (request.Id == System.Guid.Empty)
        {
            var entity = CountryCode.Create(name, request.DialCode, request.FlagUrl, by, _clock);
            await _repo.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return _msg.Ok(
                CCE.Application.Lookups.Queries.ListCountryCodes.ListCountryCodesQueryHandler.MapToDto(entity),
                "LOOKUP_CREATED");
        }
        else
        {
            var entity = await _repo.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
                return _msg.CountryCodeNotFound<CountryCodeDto>();

            entity.Update(name, request.DialCode, request.FlagUrl, request.IsActive, by, _clock);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return _msg.Ok(
                CCE.Application.Lookups.Queries.ListCountryCodes.ListCountryCodesQueryHandler.MapToDto(entity),
                "LOOKUP_UPDATED");
        }
    }
}
