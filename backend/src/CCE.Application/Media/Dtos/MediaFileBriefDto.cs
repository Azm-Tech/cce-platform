namespace CCE.Application.Media.Dtos;

public sealed record MediaFileBriefDto(
    System.Guid Id,
    string StorageKey,
    string Url);
