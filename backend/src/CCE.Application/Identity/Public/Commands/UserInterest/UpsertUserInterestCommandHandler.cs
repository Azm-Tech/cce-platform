using System.Linq;
using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using MediatR;

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

        var oldInterests = user.Interests.ToList();
        var rawList = request.Interests ?? System.Array.Empty<string>();

        var normalizedNew = rawList
            .Select(static s => s?.Trim() ?? string.Empty)
            .Where(static s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var oldSet = new HashSet<string>(oldInterests, StringComparer.OrdinalIgnoreCase);
        var newSet = new HashSet<string>(normalizedNew, StringComparer.OrdinalIgnoreCase);

        if (oldSet.SetEquals(newSet))
        {
            return _msg.InterestUpserted(new UpsertUserInterestResult(
                user.Interests,
                System.Array.Empty<string>(),
                System.Array.Empty<string>()));
        }

        user.UpdateInterests(normalizedNew);

        var added = normalizedNew.Except(oldInterests, StringComparer.OrdinalIgnoreCase).ToList();
        var removed = oldInterests.Except(normalizedNew, StringComparer.OrdinalIgnoreCase).ToList();

        _service.Update(user);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.InterestUpserted(new UpsertUserInterestResult(
            user.Interests,
            added,
            removed));
    }
}
