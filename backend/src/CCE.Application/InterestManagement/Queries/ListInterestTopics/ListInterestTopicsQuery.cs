using CCE.Application.Common;
using CCE.Application.InterestManagement.Dtos;
using MediatR;

namespace CCE.Application.InterestManagement.Queries.ListInterestTopics;

public sealed record ListInterestTopicsQuery : IRequest<Response<IReadOnlyList<InterestTopicDto>>>;