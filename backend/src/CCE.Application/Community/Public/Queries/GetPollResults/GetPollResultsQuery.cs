using CCE.Application.Common;
using CCE.Application.Community.Public.Dtos;
using MediatR;

namespace CCE.Application.Community.Public.Queries.GetPollResults;

public sealed record GetPollResultsQuery(Guid PollId) : IRequest<Response<PollResultsDto>>;
