using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Community.Commands.DeleteTopic;

public sealed record DeleteTopicCommand(System.Guid Id) : IRequest<Response<VoidData>>;
