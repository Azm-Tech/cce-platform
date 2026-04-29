using MediatR;

namespace CCE.Application.Content.Commands.DeleteEvent;

public sealed record DeleteEventCommand(System.Guid Id) : IRequest<Unit>;
