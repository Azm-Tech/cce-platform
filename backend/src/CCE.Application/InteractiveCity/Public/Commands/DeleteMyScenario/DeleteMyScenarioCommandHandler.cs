using CCE.Application.Common;
using CCE.Application.Messages;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.InteractiveCity.Public.Commands.DeleteMyScenario;

public sealed class DeleteMyScenarioCommandHandler : IRequestHandler<DeleteMyScenarioCommand, Response<VoidData>>
{
    private readonly ICityScenarioService _service;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;

    public DeleteMyScenarioCommandHandler(ICityScenarioService service, ISystemClock clock, MessageFactory msg)
    {
        _service = service;
        _clock = clock;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(DeleteMyScenarioCommand request, CancellationToken cancellationToken)
    {
        var scenario = await _service.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);

        // Return 404 whether the scenario doesn't exist OR isn't owned by this user (don't leak ownership).
        if (scenario is null || scenario.UserId != request.UserId)
            throw new System.Collections.Generic.KeyNotFoundException(
                $"Scenario {request.Id} not found.");

        scenario.SoftDelete(request.UserId, _clock);
        await _service.UpdateAsync(scenario, cancellationToken).ConfigureAwait(false);

        return _msg.Ok(MessageKeys.General.SUCCESS_DELETED);
    }
}
