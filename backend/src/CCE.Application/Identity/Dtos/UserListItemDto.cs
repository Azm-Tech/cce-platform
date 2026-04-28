namespace CCE.Application.Identity.Dtos;

/// <summary>
/// Compact user row for the admin user-list grid.
/// </summary>
public sealed record UserListItemDto(
    System.Guid Id,
    string? Email,
    string? UserName,
    IReadOnlyList<string> Roles,
    bool IsActive);
