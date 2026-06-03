using CCE.Domain.Content;
using CCE.Domain.Country;

namespace CCE.Application.Content.Dtos;

public sealed record CountryContentRequestDto(
    System.Guid Id,
    System.Guid CountryId,
    System.Guid RequestedById,
    ContentKind Kind,
    CountryContentRequestStatus Status,
    string ProposedTitleAr,
    string ProposedTitleEn,
    string ProposedDescriptionAr,
    string ProposedDescriptionEn,
    ResourceType? ProposedResourceType,
    System.Guid? ProposedAssetFileId,
    System.Guid? ProposedTopicId,
    System.DateTimeOffset? ProposedStartsOn,
    System.DateTimeOffset? ProposedEndsOn,
    string? ProposedLocationAr,
    string? ProposedLocationEn,
    string? ProposedOnlineMeetingUrl,
    System.DateTimeOffset SubmittedOn,
    string? AdminNotesAr,
    string? AdminNotesEn,
    System.Guid? ProcessedById,
    System.DateTimeOffset? ProcessedOn);
