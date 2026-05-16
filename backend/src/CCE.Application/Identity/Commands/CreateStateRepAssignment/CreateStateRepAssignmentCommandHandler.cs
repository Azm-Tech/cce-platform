using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Identity.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Identity;
using MediatR;

namespace CCE.Application.Identity.Commands.CreateStateRepAssignment;

public sealed class CreateStateRepAssignmentCommandHandler
    : IRequestHandler<CreateStateRepAssignmentCommand, Response<StateRepAssignmentDto>>
{
    private readonly ICceDbContext _db;
    private readonly IStateRepAssignmentRepository _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;

    public CreateStateRepAssignmentCommandHandler(
        ICceDbContext db,
        IStateRepAssignmentRepository service,
        ICurrentUserAccessor currentUser,
        ISystemClock clock,
        MessageFactory msg)
    {
        _db = db;
        _service = service;
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
            return _msg.UserNotFound<StateRepAssignmentDto>();
        }

        var countryExists = await ExistsAsync(_db.Countries.Where(c => c.Id == request.CountryId), cancellationToken).ConfigureAwait(false);
        if (!countryExists)
        {
            return _msg.NotFound<StateRepAssignmentDto>("COUNTRY_NOT_FOUND");
        }

        var assignedById = _currentUser.GetUserId();
        if (assignedById is null)
        {
            return _msg.NotAuthenticated<StateRepAssignmentDto>();
        }

        var assignment = StateRepresentativeAssignment.Assign(request.UserId, request.CountryId, assignedById.Value, _clock);
        await _service.AddAsync(assignment, cancellationToken).ConfigureAwait(false);
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
            IsActive: true), "STATE_REP_ASSIGNMENT_CREATED");
    }

    private static async Task<bool> ExistsAsync<T>(IQueryable<T> query, CancellationToken ct)
    {
        var list = await query.Take(1).ToListAsyncEither(ct).ConfigureAwait(false);
        return list.Count > 0;
    }
}
