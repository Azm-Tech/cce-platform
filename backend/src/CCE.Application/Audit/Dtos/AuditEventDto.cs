namespace CCE.Application.Audit.Dtos;

public sealed record AuditEventDto(
    System.Guid Id,
    System.DateTimeOffset OccurredOn,
    string Actor,
    string Action,
    string Resource,
    System.Guid CorrelationId,
    string? Diff);
