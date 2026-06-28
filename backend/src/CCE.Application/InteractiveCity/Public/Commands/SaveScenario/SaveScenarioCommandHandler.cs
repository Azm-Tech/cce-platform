using CCE.Application.Common;
using CCE.Application.InteractiveCity.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.InteractiveCity;
using MediatR;

namespace CCE.Application.InteractiveCity.Public.Commands.SaveScenario;

public sealed class SaveScenarioCommandHandler : IRequestHandler<SaveScenarioCommand, Response<CityScenarioDto>>
{
    private readonly ICityScenarioService _service;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;

    public SaveScenarioCommandHandler(ICityScenarioService service, ISystemClock clock, MessageFactory msg)
    {
        _service = service;
        _clock = clock;
        _msg = msg;
    }

    public async Task<Response<CityScenarioDto>> Handle(SaveScenarioCommand request, CancellationToken cancellationToken)
    {
        var scenario = CityScenario.Create(
            request.UserId,
            request.NameAr,
            request.NameEn,
            request.CityType,
            request.TargetYear,
            request.ConfigurationJson,
            _clock);

        await _service.SaveAsync(scenario, cancellationToken).ConfigureAwait(false);

        var dto = new CityScenarioDto(
            scenario.Id,
            scenario.NameAr,
            scenario.NameEn,
            scenario.CityType,
            scenario.TargetYear,
            scenario.ConfigurationJson,
            scenario.CreatedOn,
            scenario.LastModifiedOn);
        return _msg.Ok(dto, MessageKeys.General.SUCCESS_CREATED);
    }
}
