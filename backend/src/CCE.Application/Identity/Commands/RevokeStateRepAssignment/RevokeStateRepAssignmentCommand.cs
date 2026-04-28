using MediatR;

namespace CCE.Application.Identity.Commands.RevokeStateRepAssignment;

/// <summary>
/// Revokes (soft-deletes) the given state-rep assignment.
/// Returns Unit; the endpoint maps that to HTTP 204 No Content.
/// </summary>
public sealed record RevokeStateRepAssignmentCommand(System.Guid Id) : IRequest<Unit>;
