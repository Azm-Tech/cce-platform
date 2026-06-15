using CCE.Application.Common;
using MediatR;

namespace CCE.Application.InteractiveMaps.Commands.DeleteInteractiveMap;

public sealed record DeleteInteractiveMapCommand(System.Guid Id) : IRequest<Response<VoidData>>;
