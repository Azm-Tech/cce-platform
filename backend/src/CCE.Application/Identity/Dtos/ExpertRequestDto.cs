using CCE.Domain.Identity;

namespace CCE.Application.Identity.Dtos;

public sealed record ExpertRequestDto(
    System.Guid Id,
    System.Guid RequestedById,
    string? RequestedByUserName,
    string RequestedBioAr,
    string RequestedBioEn,
    IReadOnlyList<string> RequestedTags,
    System.DateTimeOffset SubmittedOn,
    ExpertRegistrationStatus Status,
    System.Guid? ProcessedById,
    System.DateTimeOffset? ProcessedOn,
    string? RejectionReasonAr,
    string? RejectionReasonEn);
