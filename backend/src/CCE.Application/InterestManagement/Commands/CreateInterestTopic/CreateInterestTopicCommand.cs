using CCE.Application.Common;
using CCE.Application.InterestManagement.Dtos;
using MediatR;

namespace CCE.Application.InterestManagement.Commands.CreateInterestTopic;

public sealed record CreateInterestTopicCommand(
    string NameAr,
    string NameEn) : IRequest<Response<InterestTopicDto>>;
