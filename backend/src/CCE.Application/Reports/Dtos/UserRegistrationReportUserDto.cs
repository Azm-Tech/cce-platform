namespace CCE.Application.Reports.Dtos;

public sealed record UserRegistrationReportUserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string JobTitle,
    string OrganizationName,
    string? PhoneNumber
);
