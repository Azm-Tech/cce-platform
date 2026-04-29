using MediatR;

namespace CCE.Application.Community.Commands.DeleteTopic;

public sealed record DeleteTopicCommand(System.Guid Id) : IRequest<Unit>;
