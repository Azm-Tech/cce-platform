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

        var added = user.ToggleInterest(request.Interest);

        _service.Update(user);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.InterestUpserted(new UpsertUserInterestResult(
            user.Interests,
            added));
    }
}
