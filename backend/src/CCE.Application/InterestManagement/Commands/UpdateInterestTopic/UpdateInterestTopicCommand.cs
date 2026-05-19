using CCE.Application.Common;
using CCE.Application.InterestManagement.Dtos;
using MediatR;

namespace CCE.Application.InterestManagement.Commands.UpdateInterestTopic;

public sealed record UpdateInterestTopicCommand(
    System.Guid Id,
    string NameAr,
    string NameEn) : IRequest<Response<InterestTopicDto>>;
