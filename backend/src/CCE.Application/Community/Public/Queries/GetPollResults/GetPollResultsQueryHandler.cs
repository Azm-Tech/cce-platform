using System.Collections.Generic;
using System.Linq;
using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Messages;

using CCE.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Community.Public.Queries.GetPollResults;

/// <summary>
/// Returns poll results with per-option counts + percentages. When <c>ShowResultsBeforeClose</c> is
/// false and the poll is still open, tallies are hidden (ResultsVisible=false, counts zeroed).
/// </summary>
public sealed class GetPollResultsQueryHandler
    : IRequestHandler<GetPollResultsQuery, Response<PollResultsDto>>
{
    private readonly ICceDbContext _db;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;

    public GetPollResultsQueryHandler(ICceDbContext db, ISystemClock clock, MessageFactory msg)
    {
        _db = db;
        _clock = clock;
        _msg = msg;
    }

    public async Task<Response<PollResultsDto>> Handle(GetPollResultsQuery request, CancellationToken cancellationToken)
    {
        var poll = await _db.Polls
            .Where(p => p.Id == request.PollId)
            .Select(p => new
            {
                p.Id,
                p.Deadline,
                p.AllowMultiple,
                p.ShowResultsBeforeClose,
                Options = p.Options.OrderBy(o => o.SortOrder)
                    .Select(o => new { o.Id, o.Label, o.VoteCount }).ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (poll is null) return _msg.NotFound<PollResultsDto>(MessageKeys.Community.POLL_NOT_FOUND);

        var isClosed = _clock.UtcNow >= poll.Deadline;
        var resultsVisible = isClosed || poll.ShowResultsBeforeClose;
        var total = poll.Options.Sum(o => o.VoteCount);

        var options = poll.Options.Select(o => new PollOptionResultDto(
            o.Id,
            o.Label,
            resultsVisible ? o.VoteCount : 0,
            resultsVisible && total > 0 ? System.Math.Round(o.VoteCount * 100.0 / total, 1) : 0))
            .ToList();

        var dto = new PollResultsDto(poll.Id, poll.Deadline, isClosed, poll.AllowMultiple,
            resultsVisible, resultsVisible ? total : 0, options);
        return _msg.Ok(dto, MessageKeys.General.ITEMS_LISTED);
    }
}
