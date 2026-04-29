using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.InteractiveCity.Public.Dtos;
using CCE.Domain.InteractiveCity;
using MediatR;

namespace CCE.Application.InteractiveCity.Public.Queries.ListCityTechnologies;

public sealed class ListCityTechnologiesQueryHandler
    : IRequestHandler<ListCityTechnologiesQuery, System.Collections.Generic.IReadOnlyList<CityTechnologyDto>>
{
    private readonly ICceDbContext _db;

    public ListCityTechnologiesQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<System.Collections.Generic.IReadOnlyList<CityTechnologyDto>> Handle(
        ListCityTechnologiesQuery request, CancellationToken cancellationToken)
    {
        var techs = await _db.CityTechnologies
            .Where(t => t.IsActive)
            .OrderBy(t => t.CategoryEn)
            .ThenBy(t => t.NameEn)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        return techs.Select(MapToDto).ToList();
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
