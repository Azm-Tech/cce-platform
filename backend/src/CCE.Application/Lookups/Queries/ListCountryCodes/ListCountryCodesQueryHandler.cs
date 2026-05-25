using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Application.PlatformSettings.Dtos;
using CCE.Domain.Lookups;
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
        IQueryable<CountryCode> query = _db.CountryCodes;

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(c =>
                c.Name.Ar.Contains(term) ||
                c.Name.En.Contains(term) ||
                c.DialCode.Contains(term));
        }

        query = query
            .WhereIf(request.IsActive.HasValue, c => c.IsActive == request.IsActive!.Value)
            .OrderBy(c => c.Name.En);

        var items = await query
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<CountryCodeDto> dtos = items.Select(MapToDto).ToList();
        return _msg.Ok(dtos, "ITEMS_LISTED");
    }

    internal static CountryCodeDto MapToDto(CountryCode c) =>
        new(c.Id, new LocalizedTextDto(c.Name.Ar, c.Name.En), c.DialCode, c.IsActive);
}
