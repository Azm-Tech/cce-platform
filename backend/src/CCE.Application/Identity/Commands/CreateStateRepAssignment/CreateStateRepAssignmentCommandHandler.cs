using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Identity.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Country;
using CCE.Domain.Identity;
using MediatR;

namespace CCE.Application.Identity.Commands.CreateStateRepAssignment;

public sealed class CreateStateRepAssignmentCommandHandler
    : IRequestHandler<CreateStateRepAssignmentCommand, Response<StateRepAssignmentDto>>
{
    private readonly ICceDbContext _db;
    private readonly IStateRepAssignmentRepository _service;
    private readonly IRepository<CountryProfile, System.Guid> _profiles;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;

    public CreateStateRepAssignmentCommandHandler(
        ICceDbContext db,
        IStateRepAssignmentRepository service,
        IRepository<CountryProfile, System.Guid> profiles,
        ICurrentUserAccessor currentUser,
        ISystemClock clock,
        MessageFactory msg)
    {
        _db = db;
        _service = service;
        _profiles = profiles;
        _currentUser = currentUser;
        _clock = clock;
        _msg = msg;
    }

    public async Task<Response<StateRepAssignmentDto>> Handle(
        CreateStateRepAssignmentCommand request,
        CancellationToken cancellationToken)
    {
        var userExists = await ExistsAsync(_db.Users.Where(u => u.Id == request.UserId), cancellationToken).ConfigureAwait(false);
        if (!userExists)
        {
            return _msg.NotFound<StateRepAssignmentDto>(MessageKeys.Identity.USER_NOT_FOUND);
        }

        var countryExists = await ExistsAsync(_db.Countries.Where(c => c.Id == request.CountryId), cancellationToken).ConfigureAwait(false);
        if (!countryExists)
        {
            return _msg.NotFound<StateRepAssignmentDto>(MessageKeys.Country.COUNTRY_NOT_FOUND);
        }

        var assignedById = _currentUser.GetUserId();
        if (assignedById is null)
        {
            return _msg.Unauthorized<StateRepAssignmentDto>(MessageKeys.Identity.NOT_AUTHENTICATED);
        }

        var assignment = StateRepresentativeAssignment.Assign(request.UserId, request.CountryId, assignedById.Value, _clock);
        await _service.AddAsync(assignment, cancellationToken).ConfigureAwait(false);

        // Ensure the assigned country has a profile to edit (US060/US061). If none exists yet,
        // seed an empty draft so the State Rep lands on a real record. Committed together with
        // the assignment in a single unit of work.
        var hasProfile = await ExistsAsync(
            _db.CountryProfiles.Where(p => p.CountryId == request.CountryId), cancellationToken).ConfigureAwait(false);
        if (!hasProfile)
        {
            var draft = CountryProfile.CreateDraft(request.CountryId, assignedById.Value, _clock);
            await _profiles.AddAsync(draft, cancellationToken).ConfigureAwait(false);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var userNames = await _db.Users
            .Where(u => u.Id == request.UserId)
            .Select(u => u.UserName)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var userName = userNames.FirstOrDefault();

        return _msg.Ok(new StateRepAssignmentDto(
            assignment.Id,
            assignment.UserId,
            userName,
            assignment.CountryId,
            assignment.AssignedOn,
            assignment.AssignedById,
            assignment.RevokedOn,
            assignment.RevokedById,
            IsActive: true), MessageKeys.Identity.STATE_REP_ASSIGNMENT_CREATED);
    }

    private static async Task<bool> ExistsAsync<T>(IQueryable<T> query, CancellationToken ct)
    {
        var list = await query.Take(1).ToListAsyncEither(ct).ConfigureAwait(false);
        return list.Count > 0;
    }
}
