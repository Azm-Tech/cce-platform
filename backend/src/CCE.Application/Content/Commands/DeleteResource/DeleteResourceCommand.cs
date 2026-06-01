using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Content.Commands.DeleteResource;

public sealed record DeleteResourceCommand(System.Guid Id) : IRequest<Response<VoidData>>;
