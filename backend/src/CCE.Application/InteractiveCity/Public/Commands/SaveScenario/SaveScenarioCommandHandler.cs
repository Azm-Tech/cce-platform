using CCE.Application.InteractiveCity.Public.Dtos;
using CCE.Domain.Common;
using CCE.Domain.InteractiveCity;
using MediatR;

namespace CCE.Application.InteractiveCity.Public.Commands.SaveScenario;

public sealed class SaveScenarioCommandHandler : IRequestHandler<SaveScenarioCommand, CityScenarioDto>
{
    private readonly ICityScenarioService _service;
    private readonly ISystemClock _clock;

    public SaveScenarioCommandHandler(ICityScenarioService service, ISystemClock clock)
    {
        _service = service;
        _clock = clock;
    }

    public async Task<CityScenarioDto> Handle(SaveScenarioCommand request, CancellationToken cancellationToken)
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

        return new CityScenarioDto(
            scenario.Id,
            scenario.NameAr,
            scenario.NameEn,
            scenario.CityType,
            scenario.TargetYear,
            scenario.ConfigurationJson,
            scenario.CreatedOn,
            scenario.LastModifiedOn);
    }
}
