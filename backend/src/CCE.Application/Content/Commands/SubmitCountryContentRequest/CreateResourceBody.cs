using CCE.Domain.Content;

namespace CCE.Application.Content.Commands.SubmitCountryContentRequest;

public sealed record CreateResourceBody(
    string TitleAr,
    string TitleEn,
    string DescriptionAr,
    string DescriptionEn,
    ResourceType ResourceType,
    System.Guid CategoryId,
    System.Guid? TopicId,
    System.Collections.Generic.List<System.Guid>? CountryIds,
    System.Guid AssetFileId) : ContentBody;
