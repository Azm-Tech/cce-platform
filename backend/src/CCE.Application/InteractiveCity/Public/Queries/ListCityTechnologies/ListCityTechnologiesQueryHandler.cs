using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.InteractiveCity.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.InteractiveCity;
using MediatR;

namespace CCE.Application.InteractiveCity.Public.Queries.ListCityTechnologies;

public sealed class ListCityTechnologiesQueryHandler
    : IRequestHandler<ListCityTechnologiesQuery, Response<System.Collections.Generic.IReadOnlyList<CityTechnologyDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public ListCityTechnologiesQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<System.Collections.Generic.IReadOnlyList<CityTechnologyDto>>> Handle(
        ListCityTechnologiesQuery request, CancellationToken cancellationToken)
    {
        var techs = await _db.CityTechnologies
            .Where(t => t.IsActive)
            .OrderBy(t => t.CategoryEn)
            .ThenBy(t => t.NameEn)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        System.Collections.Generic.IReadOnlyList<CityTechnologyDto> list = techs.Select(MapToDto).ToList();
        return _msg.Ok(list, MessageKeys.General.ITEMS_LISTED);
    }

    internal static CityTechnologyDto MapToDto(CityTechnology t) => new(
        t.Id,
        t.NameAr,
        t.NameEn,
        t.DescriptionAr,
        t.DescriptionEn,
        t.CategoryAr,
        t.CategoryEn,
        t.CarbonImpactKgPerYear,
        t.CostUsd,
        t.IconUrl);
}
