using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Application.PlatformSettings.Dtos;
using MediatR;

namespace CCE.Application.Lookups.Queries.ListCountryCodes;

public sealed class ListCountryCodesQueryHandler
    : IRequestHandler<ListCountryCodesQuery, Response<IReadOnlyList<CountryCodeDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public ListCountryCodesQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<IReadOnlyList<CountryCodeDto>>> Handle(
        ListCountryCodesQuery request,
        CancellationToken cancellationToken)
    {
        // Countries that have a dial code — covers both CCE members and world lookup entries.
        IQueryable<CCE.Domain.Country.Country> query = _db.Countries.Where(c => c.DialCode != null);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(c =>
                c.NameAr.Contains(term) ||
                c.NameEn.Contains(term) ||
                (c.DialCode != null && c.DialCode.Contains(term)));
        }

        query = query
            .WhereIf(request.IsActive.HasValue, c => c.IsActive == request.IsActive!.Value)
            .OrderBy(c => c.NameEn);

        var items = await query
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<CountryCodeDto> dtos = items.Select(MapToDto).ToList();
        return _msg.Ok(dtos, MessageKeys.General.ITEMS_LISTED);
    }

    internal static CountryCodeDto MapToDto(CCE.Domain.Country.Country c) =>
        new(c.Id, new LocalizedTextDto(c.NameAr, c.NameEn), c.DialCode!, c.FlagUrl, c.IsActive);
}
