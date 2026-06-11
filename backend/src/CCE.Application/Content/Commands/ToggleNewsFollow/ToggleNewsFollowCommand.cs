using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Content.Commands.ToggleNewsFollow;

public sealed record ToggleNewsFollowCommand : IRequest<Response<VoidData>>;
