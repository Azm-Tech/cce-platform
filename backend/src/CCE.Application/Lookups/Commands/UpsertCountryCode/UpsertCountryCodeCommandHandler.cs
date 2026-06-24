using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.Common;
using MediatR;
using CountryEntity = CCE.Domain.Country.Country;

namespace CCE.Application.Lookups.Commands.UpsertCountryCode;

public sealed class UpsertCountryCodeCommandHandler
    : IRequestHandler<UpsertCountryCodeCommand, Response<CountryCodeDto>>
{
    private readonly IRepository<CountryEntity, System.Guid> _repo;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly MessageFactory _msg;

    public UpsertCountryCodeCommandHandler(
        IRepository<CountryEntity, System.Guid> repo,
        ICceDbContext db,
        ICurrentUserAccessor currentUser,
        MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _currentUser = currentUser;
        _msg = msg;
    }

    public async Task<Response<CountryCodeDto>> Handle(
        UpsertCountryCodeCommand request,
        CancellationToken cancellationToken)
    {
        _ = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot upsert country code from a request without a user identity.");

        if (request.Id == System.Guid.Empty)
        {
            var entity = CountryEntity.RegisterLookup(request.NameAr, request.NameEn, request.DialCode, request.FlagUrl, isoAlpha2: null);
            await _repo.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return _msg.Ok(
                CCE.Application.Lookups.Queries.ListCountryCodes.ListCountryCodesQueryHandler.MapToDto(entity),
                MessageKeys.Lookups.LOOKUP_CREATED);
        }
        else
        {
            var entity = await _repo.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
                return _msg.CountryCodeNotFound<CountryCodeDto>();

            entity.UpdateLookup(request.NameAr, request.NameEn, request.DialCode, request.FlagUrl, request.IsActive);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return _msg.Ok(
                CCE.Application.Lookups.Queries.ListCountryCodes.ListCountryCodesQueryHandler.MapToDto(entity),
                MessageKeys.Lookups.LOOKUP_UPDATED);
        }
    }
}
