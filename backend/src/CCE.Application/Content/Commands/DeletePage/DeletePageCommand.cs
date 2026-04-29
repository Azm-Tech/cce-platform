using MediatR;

namespace CCE.Application.Content.Commands.DeletePage;

public sealed record DeletePageCommand(System.Guid Id) : IRequest<Unit>;
