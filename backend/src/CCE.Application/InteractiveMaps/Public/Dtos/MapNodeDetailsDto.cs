using CCE.Domain.Community;
using CCE.Domain.Content;

namespace CCE.Application.InteractiveMaps.Public.Dtos;

/// <summary>
/// Full details panel returned when a user clicks an interactive-map node.
/// </summary>
public sealed record MapNodeDetailsDto(
    MapNodeSummaryDto Node,
    MapNodeTopicDto Topic,
    IReadOnlyList<MapNodeResourceDto> Resources,
    IReadOnlyList<MapNodeNewsDto> News,
    IReadOnlyList<MapNodeEventDto> Events,
    IReadOnlyList<MapNodePostDto> Posts);

/// <summary>Core fields of the clicked node.</summary>
public sealed record MapNodeSummaryDto(
    System.Guid Id,
    string NameAr,
    string NameEn,
    string IconKey,
    System.Guid TopicId);

/// <summary>Topic linked to the node.</summary>
public sealed record MapNodeTopicDto(
    System.Guid Id,
    string NameAr,
    string NameEn,
    string DescriptionAr,
    string DescriptionEn,
    string Slug,
    string? IconUrl);

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

/// <summary>Slim post card — published posts filtered by the node's topic.</summary>
public sealed record MapNodePostDto(
    System.Guid Id,
    PostType Type,
    string? Title,
    string? Content,
    int CommentsCount,
    System.DateTimeOffset CreatedOn);
