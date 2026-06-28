using CCE.Application.Common;
using MediatR;

namespace CCE.Application.InteractiveCity.Public.Commands.DeleteMyScenario;

public sealed record DeleteMyScenarioCommand(System.Guid Id, System.Guid UserId) : IRequest<Response<VoidData>>;
