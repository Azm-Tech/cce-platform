using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.InteractiveCity.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.InteractiveCity;
using MediatR;

namespace CCE.Application.InteractiveCity.Public.Queries.ListMyScenarios;

public sealed class ListMyScenariosQueryHandler
    : IRequestHandler<ListMyScenariosQuery, Response<System.Collections.Generic.IReadOnlyList<CityScenarioDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public ListMyScenariosQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<System.Collections.Generic.IReadOnlyList<CityScenarioDto>>> Handle(
        ListMyScenariosQuery request, CancellationToken cancellationToken)
    {
        var scenarios = await _db.CityScenarios
            .Where(s => s.UserId == request.UserId)
            .OrderByDescending(s => s.LastModifiedOn)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        System.Collections.Generic.IReadOnlyList<CityScenarioDto> list = scenarios.Select(MapToDto).ToList();
        return _msg.Ok(list, MessageKeys.General.ITEMS_LISTED);
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
