using CCE.Domain.Content;

namespace CCE.Application.InteractiveMaps.Public.Dtos;

/// <summary>
/// Full details panel returned when a user clicks an interactive-map node.
/// </summary>
public sealed record MapNodeDetailsDto(
    MapNodeSummaryDto Node,
    IReadOnlyList<MapNodeResourceDto> Resources,
    IReadOnlyList<MapNodeNewsDto> News,
    IReadOnlyList<MapNodeEventDto> Events);

/// <summary>Core fields of the clicked node.</summary>
public sealed record MapNodeSummaryDto(
    System.Guid Id,
    string NameAr,
    string NameEn,
    string IconKey,
    string? TitleAr,
    string? TitleEn,
    string? DescriptionAr,
    string? DescriptionEn,
    System.Guid TopicId);

/// <summary>Slim resource card — top N recently published.</summary>
public sealed record MapNodeResourceDto(
    System.Guid Id,
    string TitleAr,
    string TitleEn,
    ResourceType ResourceType,
    string CategoryNameAr,
    string CategoryNameEn,
    System.DateTimeOffset PublishedOn);

/// <summary>Slim news card — filtered by the node's topic.</summary>
public sealed record MapNodeNewsDto(
    System.Guid Id,
    string TitleAr,
    string TitleEn,
    string? FeaturedImageUrl,
    System.DateTimeOffset PublishedOn);

/// <summary>Slim event card — upcoming events filtered by the node's topic.</summary>
public sealed record MapNodeEventDto(
    System.Guid Id,
    string TitleAr,
    string TitleEn,
    System.DateTimeOffset StartsOn,
    System.DateTimeOffset EndsOn,
    string? FeaturedImageUrl);
