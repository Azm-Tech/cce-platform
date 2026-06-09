namespace CCE.Application.InterestManagement.Dtos;

public sealed record InterestTopicDto(
    System.Guid Id,
    string NameAr,
    string NameEn,
    string Category,
    bool IsActive);
