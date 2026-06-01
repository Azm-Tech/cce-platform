using CCE.Domain.Content;

namespace CCE.Api.Internal.Endpoints;

public sealed record UpdateResourceRequest(
    string TitleAr,
    string TitleEn,
    string DescriptionAr,
    string DescriptionEn,
    ResourceType ResourceType,
    System.Guid CategoryId,
    List<System.Guid> CountryIds,
    string RowVersion);
