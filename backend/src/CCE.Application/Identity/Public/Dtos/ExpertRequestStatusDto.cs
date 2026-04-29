using CCE.Domain.Identity;

namespace CCE.Application.Identity.Public.Dtos;

public sealed record ExpertRequestStatusDto(
    System.Guid Id,
    System.Guid RequestedById,
    string RequestedBioAr,
    string RequestedBioEn,
    IReadOnlyList<string> RequestedTags,
    System.DateTimeOffset SubmittedOn,
    ExpertRegistrationStatus Status,
    System.DateTimeOffset? ProcessedOn,
    string? RejectionReasonAr,
    string? RejectionReasonEn);
