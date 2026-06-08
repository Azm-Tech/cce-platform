using System.Collections.Generic;
using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Community.Commands.CastPollVote;

public sealed record CastPollVoteCommand(Guid PollId, IReadOnlyList<Guid> OptionIds)
    : IRequest<Response<VoidData>>;

public sealed record CastPollVoteRequest(IReadOnlyList<Guid>? OptionIds);
