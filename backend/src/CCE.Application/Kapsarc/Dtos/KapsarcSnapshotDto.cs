namespace CCE.Application.Kapsarc.Dtos;

public sealed record KapsarcSnapshotDto(
    System.Guid Id,
    System.Guid CountryId,
    string Classification,
    decimal PerformanceScore,
    decimal TotalIndex,
    System.DateTimeOffset SnapshotTakenOn,
    string? SourceVersion);
