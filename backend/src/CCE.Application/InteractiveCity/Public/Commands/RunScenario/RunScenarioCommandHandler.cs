using System.Text.Json;
using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.InteractiveCity.Public.Dtos;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.InteractiveCity.Public.Commands.RunScenario;

public sealed class RunScenarioCommandHandler : IRequestHandler<RunScenarioCommand, Response<CityScenarioRunResultDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public RunScenarioCommandHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<CityScenarioRunResultDto>> Handle(RunScenarioCommand request, CancellationToken cancellationToken)
    {
        // Parse configurationJson — on failure return zero totals (don't expose 500 to anonymous callers).
        List<System.Guid> technologyIds;
        try
        {
            using var doc = JsonDocument.Parse(request.ConfigurationJson);
            if (!doc.RootElement.TryGetProperty("technologyIds", out var idsElement)
                || idsElement.ValueKind != JsonValueKind.Array)
            {
                return _msg.Ok(InvalidConfig(), MessageKeys.General.SUCCESS_OPERATION);
            }

            technologyIds = new List<System.Guid>();
            foreach (var el in idsElement.EnumerateArray())
            {
                if (!el.TryGetGuid(out var id))
                    return _msg.Ok(InvalidConfig(), MessageKeys.General.SUCCESS_OPERATION);
                technologyIds.Add(id);
            }
        }
        catch (JsonException)
        {
            return _msg.Ok(InvalidConfig(), MessageKeys.General.SUCCESS_OPERATION);
        }

        if (technologyIds.Count == 0)
        {
            var noTech = new CityScenarioRunResultDto(0m, 0m,
                "لا توجد تقنيات محددة",
                "No technologies selected");
            return _msg.Ok(noTech, MessageKeys.General.SUCCESS_OPERATION);
        }

        var techs = await _db.CityTechnologies
            .Where(t => technologyIds.Contains(t.Id) && t.IsActive)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        var totalCarbon = techs.Sum(t => t.CarbonImpactKgPerYear);
        var totalCost = techs.Sum(t => t.CostUsd);

        var dto = new CityScenarioRunResultDto(
            totalCarbon,
            totalCost,
            $"إجمالي تأثير الكربون: {totalCarbon} كغ/سنة، التكلفة الإجمالية: {totalCost} دولار",
            $"Total carbon impact: {totalCarbon} kg/year, total cost: {totalCost} USD");
        return _msg.Ok(dto, MessageKeys.General.SUCCESS_OPERATION);
    }

    private static CityScenarioRunResultDto InvalidConfig() =>
        new(0m, 0m, "تكوين غير صالح", "Invalid configuration");
}
