using CCE.Application.Common;
using MediatR;

namespace CCE.Application.InterestManagement.Commands.DeleteInterestTopic;

public sealed record DeleteInterestTopicCommand(System.Guid Id) : IRequest<Response<VoidData>>;
