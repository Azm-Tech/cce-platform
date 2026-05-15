using CCE.Domain.Common;

namespace CCE.Domain.Identity;

/// <summary>
/// Persistent record of a state-representative role assignment to a user, scoped by country.
/// Soft-deletable: revoking the assignment sets <see cref="RevokedOn"/>/<see cref="RevokedById"/>
/// AND marks the row deleted (so the unique-active-assignment filtered index ignores it).
/// </summary>
[Audited]
public sealed class StateRepresentativeAssignment : SoftDeletableEntity<System.Guid>
{
    private StateRepresentativeAssignment(
        System.Guid id,
        System.Guid userId,
        System.Guid countryId,
        System.Guid assignedById,
        System.DateTimeOffset assignedOn) : base(id)
    {
        UserId = userId;
        CountryId = countryId;
        AssignedById = assignedById;
        AssignedOn = assignedOn;
    }

    /// <summary>FK to <c>User.Id</c>.</summary>
    public System.Guid UserId { get; private set; }

    /// <summary>FK to <c>Country.Id</c>.</summary>
    public System.Guid CountryId { get; private set; }

    /// <summary>UTC moment the assignment was created.</summary>
    public System.DateTimeOffset AssignedOn { get; private set; }

    /// <summary>Admin <c>User.Id</c> who created the assignment.</summary>
    public System.Guid AssignedById { get; private set; }

    /// <summary>UTC moment the assignment was revoked; null if still active.</summary>
    public System.DateTimeOffset? RevokedOn { get; private set; }

    /// <summary>Admin <c>User.Id</c> who revoked; null if still active.</summary>
    public System.Guid? RevokedById { get; private set; }

    /// <summary>
    /// Factory: create a new active assignment. The "unique active per (User, Country)" invariant
    /// is checked at the persistence layer (Phase 08 filtered unique index).
    /// </summary>
    public static StateRepresentativeAssignment Assign(
        System.Guid userId,
        System.Guid countryId,
        System.Guid assignedById,
        ISystemClock clock)
    {
        if (userId == System.Guid.Empty)
        {
            throw new DomainException("UserId is required.");
        }
        if (countryId == System.Guid.Empty)
        {
            throw new DomainException("CountryId is required.");
        }
        if (assignedById == System.Guid.Empty)
        {
            throw new DomainException("AssignedById is required.");
        }
        return new StateRepresentativeAssignment(
            id: System.Guid.NewGuid(),
            userId: userId,
            countryId: countryId,
            assignedById: assignedById,
            assignedOn: clock.UtcNow);
    }

    /// <summary>
    /// Revoke this assignment. Sets <see cref="RevokedOn"/>/<see cref="RevokedById"/> and marks the
    /// row soft-deleted. Throws if already revoked.
    /// </summary>
    public void Revoke(System.Guid revokedById, ISystemClock clock)
    {
        if (IsDeleted || RevokedOn is not null)
        {
            throw new DomainException("Assignment is already revoked.");
        }
        if (revokedById == System.Guid.Empty)
        {
            throw new DomainException("RevokedById is required.");
        }
        RevokedOn = clock.UtcNow;
        RevokedById = revokedById;
        SoftDelete(revokedById, clock);
    }
}
