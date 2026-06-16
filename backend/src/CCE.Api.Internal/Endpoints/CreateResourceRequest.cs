using CCE.Domain.Content;

namespace CCE.Api.Internal.Endpoints;

public sealed record CreateResourceRequest(
    string TitleAr,
    string TitleEn,
    string DescriptionAr,
    string DescriptionEn,
    ResourceType ResourceType,
    System.Guid CategoryId,
    System.Guid? CountryId,
    System.Guid AssetFileId,
    List<System.Guid> CountryIds,
    System.Guid? KnowledgeLevelId = null,
    System.Guid? JobSectorId = null);
