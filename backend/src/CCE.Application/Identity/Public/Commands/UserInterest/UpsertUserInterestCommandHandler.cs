using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.InterestManagement.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Identity.Public.Commands.UserInterest;

public sealed class UpsertUserInterestCommandHandler
    : IRequestHandler<UpsertUserInterestCommand, Response<UpsertUserInterestResult>>
{
    private readonly IUserProfileRepository _service;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public UpsertUserInterestCommandHandler(
        IUserProfileRepository service,
        ICceDbContext db,
        MessageFactory msg)
    {
        _service = service;
        _db = db;
        _msg = msg;
    }

    public async Task<Response<UpsertUserInterestResult>> Handle(
        UpsertUserInterestCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _service.FindAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
            return _msg.UserNotFound<UpsertUserInterestResult>();

        var newIds = (request.InterestTopicIds ?? System.Array.Empty<System.Guid>())
            .Distinct()
            .ToHashSet();

        var oldIds = user.UserInterestTopics
            .Select(uit => uit.InterestTopicId)
            .ToHashSet();

        var toRemove = user.UserInterestTopics
            .Where(uit => !newIds.Contains(uit.InterestTopicId))
            .ToList();

        var toAddIds = newIds
            .Where(id => !oldIds.Contains(id))
            .ToList();

        foreach (var remove in toRemove)
            user.UserInterestTopics.Remove(remove);

        foreach (var id in toAddIds)
            user.UserInterestTopics.Add(new UserInterestTopic
            {
                UserId = user.Id,
                InterestTopicId = id
            });

        if (toRemove.Count > 0 || toAddIds.Count > 0)
        {
            _service.Update(user);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var currentTopicIds = user.UserInterestTopics
            .Select(uit => uit.InterestTopicId)
            .ToHashSet();
        var currentTopics = await _db.InterestTopics
            .Where(t => currentTopicIds.Contains(t.Id))
            .Select(t => new InterestTopicDto(t.Id, t.NameAr, t.NameEn, t.IsActive))
            .ToListAsync(cancellationToken);

        return _msg.InterestUpserted(new UpsertUserInterestResult(
            currentTopics,
            toAddIds,
            toRemove.Select(r => r.InterestTopicId).ToList()));
    }
}
