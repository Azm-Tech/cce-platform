using CCE.Application.Common;
using MediatR;

namespace CCE.Application.InteractiveMaps.Commands.DeleteInteractiveMapNode;

public sealed record DeleteInteractiveMapNodeCommand(System.Guid MapId, System.Guid Id) : IRequest<Response<VoidData>>;
