using CCE.Domain.Content;

namespace CCE.Api.Internal.Endpoints;

public sealed record SubmitResourceRequest(
    System.Guid CountryId,
    string TitleAr,
    string TitleEn,
    string DescriptionAr,
    string DescriptionEn,
    ResourceType ResourceType,
    System.Guid AssetFileId);
