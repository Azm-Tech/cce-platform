using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.InteractiveCity.Public.Dtos;
using CCE.Domain.InteractiveCity;
using MediatR;

namespace CCE.Application.InteractiveCity.Public.Queries.ListMyScenarios;

public sealed class ListMyScenariosQueryHandler
    : IRequestHandler<ListMyScenariosQuery, System.Collections.Generic.IReadOnlyList<CityScenarioDto>>
{
    private readonly ICceDbContext _db;

    public ListMyScenariosQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<System.Collections.Generic.IReadOnlyList<CityScenarioDto>> Handle(
        ListMyScenariosQuery request, CancellationToken cancellationToken)
    {
        var scenarios = await _db.CityScenarios
            .Where(s => s.UserId == request.UserId)
            .OrderByDescending(s => s.LastModifiedOn)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        return scenarios.Select(MapToDto).ToList();
    }

    internal static CityScenarioDto MapToDto(CityScenario s) => new(
        s.Id,
        s.NameAr,
        s.NameEn,
        s.CityType,
        s.TargetYear,
        s.ConfigurationJson,
        s.CreatedOn,
        s.LastModifiedOn);
}
