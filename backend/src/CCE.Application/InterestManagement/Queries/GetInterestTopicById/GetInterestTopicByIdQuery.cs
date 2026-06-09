using CCE.Application.Common;
using CCE.Application.InterestManagement.Dtos;
using MediatR;

namespace CCE.Application.InterestManagement.Queries.GetInterestTopicById;

public sealed record GetInterestTopicByIdQuery(System.Guid Id) : IRequest<Response<InterestTopicDto>>;
