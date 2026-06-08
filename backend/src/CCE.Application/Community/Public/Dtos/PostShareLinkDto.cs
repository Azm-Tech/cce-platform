namespace CCE.Application.Community.Public.Dtos;

/// <summary>US025 — a shareable link for a post.</summary>
public sealed record PostShareLinkDto(System.Guid PostId, string Url);
