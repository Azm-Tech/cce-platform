using System.Linq;
using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Realtime;
using CCE.Application.Errors;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Commands.CastPollVote;

/// <summary>
/// Casts a poll vote (§A.1): rejects after the deadline, enforces single/multiple per poll settings,
/// records votes and bumps denormalized option counts, committed once via the context (UoW).
/// </summary>
public sealed class CastPollVoteCommandHandler
    : IRequestHandler<CastPollVoteCommand, Response<VoidData>>
{
    private readonly IPollRepository _repo;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;
    private readonly ICommunityRealtimePublisher _realtime;

    public CastPollVoteCommandHandler(
        IPollRepository repo, ICceDbContext db, ICurrentUserAccessor currentUser,
        ISystemClock clock, MessageFactory msg, ICommunityRealtimePublisher realtime)
    {
        _repo = repo;
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _msg = msg;
        _realtime = realtime;
    }

    public async Task<Response<VoidData>> Handle(CastPollVoteCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();
        if (userId is null || userId == Guid.Empty) return _msg.NotAuthenticated<VoidData>();

        var optionIds = request.OptionIds.Distinct().ToList();
        if (optionIds.Count == 0)
            return _msg.BusinessRule<VoidData>(ApplicationErrors.Validation.REQUIRED_FIELD);

        var poll = await _repo.GetWithOptionsAsync(request.PollId, cancellationToken).ConfigureAwait(false);
        if (poll is null) return _msg.NotFound<VoidData>(ApplicationErrors.Community.POLL_NOT_FOUND);
        if (poll.IsClosed(_clock)) return _msg.BusinessRule<VoidData>(ApplicationErrors.Community.POLL_CLOSED);
        if (!poll.AllowMultiple && optionIds.Count > 1)
            return _msg.BusinessRule<VoidData>(ApplicationErrors.Validation.INVALID_FORMAT);

        var existingVotes = await _repo.RemoveVotesAsync(poll.Id, userId.Value, cancellationToken).ConfigureAwait(false);
        foreach (var oldVote in existingVotes)
        {
            var option = poll.FindOption(oldVote.PollOptionId);
            if (option is not null) option.DecrementVotes();
        }

        foreach (var optionId in optionIds)
        {
            var option = poll.FindOption(optionId);
            if (option is null) return _msg.NotFound<VoidData>(ApplicationErrors.Community.POLL_NOT_FOUND);
            _repo.AddVote(PollVote.Cast(poll.Id, option.Id, userId.Value, _clock));
            option.IncrementVotes();
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _realtime.PublishToPostAsync(poll.PostId, RealtimeEvents.PollResultsChanged,
            new { pollId = poll.Id, poll.PostId }, cancellationToken).ConfigureAwait(false);

        return _msg.Ok(ApplicationErrors.General.SUCCESS_OPERATION);
    }
}
