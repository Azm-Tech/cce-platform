namespace CCE.Application.Identity.Dtos;

/// <summary>
/// State-representative-to-country assignment row for the admin grid.
/// <c>UserName</c> is joined from the users table; <c>CountryId</c> is returned as a Guid only
/// (the country join lands in Phase 06 when the Country entity gains an admin endpoint).
/// </summary>
public sealed record StateRepAssignmentDto(
    System.Guid Id,
    System.Guid UserId,
    string? UserName,
    System.Guid CountryId,
    System.DateTimeOffset AssignedOn,
    System.Guid AssignedById,
    System.DateTimeOffset? RevokedOn,
    System.Guid? RevokedById,
    bool IsActive);
