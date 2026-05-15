using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Identity.Dtos;
using CCE.Domain.Common;
using CCE.Domain.Identity;
using MediatR;

namespace CCE.Application.Identity.Commands.CreateStateRepAssignment;

public sealed class CreateStateRepAssignmentCommandHandler
    : IRequestHandler<CreateStateRepAssignmentCommand, Result<StateRepAssignmentDto>>
{
    private readonly ICceDbContext _db;
    private readonly IStateRepAssignmentRepository _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly CCE.Application.Common.Errors _errors;

    public CreateStateRepAssignmentCommandHandler(
        ICceDbContext db,
        IStateRepAssignmentRepository service,
        ICurrentUserAccessor currentUser,
        ISystemClock clock,
        CCE.Application.Common.Errors errors)
    {
        _db = db;
        _service = service;
        _currentUser = currentUser;
        _clock = clock;
        _errors = errors;
    }

    public async Task<Result<StateRepAssignmentDto>> Handle(
        CreateStateRepAssignmentCommand request,
        CancellationToken cancellationToken)
    {
        // Verify user exists.
        var userExists = await ExistsAsync(_db.Users.Where(u => u.Id == request.UserId), cancellationToken).ConfigureAwait(false);
        if (!userExists)
        {
            return _errors.UserNotFound();
        }

        // Verify country exists.
        var countryExists = await ExistsAsync(_db.Countries.Where(c => c.Id == request.CountryId), cancellationToken).ConfigureAwait(false);
        if (!countryExists)
        {
            return _errors.CountryNotFound();
        }

        var assignedById = _currentUser.GetUserId();
        if (assignedById is null)
        {
            return _errors.NotAuthenticated();
        }

        var assignment = StateRepresentativeAssignment.Assign(request.UserId, request.CountryId, assignedById.Value, _clock);
        await _service.SaveAsync(assignment, cancellationToken).ConfigureAwait(false);

        // Build the DTO — look up UserName for the assigned user.
        var userNames = await _db.Users
            .Where(u => u.Id == request.UserId)
            .Select(u => u.UserName)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var userName = userNames.FirstOrDefault();

        return new StateRepAssignmentDto(
            assignment.Id,
            assignment.UserId,
            userName,
            assignment.CountryId,
            assignment.AssignedOn,
            assignment.AssignedById,
            assignment.RevokedOn,
            assignment.RevokedById,
            IsActive: true);
    }

    private static async Task<bool> ExistsAsync<T>(IQueryable<T> query, CancellationToken ct)
    {
        var list = await query.Take(1).ToListAsyncEither(ct).ConfigureAwait(false);
        return list.Count > 0;
    }
}
