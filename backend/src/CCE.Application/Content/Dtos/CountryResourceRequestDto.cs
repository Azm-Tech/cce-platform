using CCE.Domain.Content;
using CCE.Domain.Country;

namespace CCE.Application.Content.Dtos;

public sealed record CountryResourceRequestDto(
    System.Guid Id,
    System.Guid CountryId,
    System.Guid RequestedById,
    CountryResourceRequestStatus Status,
    string ProposedTitleAr,
    string ProposedTitleEn,
    string ProposedDescriptionAr,
    string ProposedDescriptionEn,
    ResourceType ProposedResourceType,
    System.Guid ProposedAssetFileId,
    System.DateTimeOffset SubmittedOn,
    string? AdminNotesAr,
    string? AdminNotesEn,
    System.Guid? ProcessedById,
    System.DateTimeOffset? ProcessedOn);
