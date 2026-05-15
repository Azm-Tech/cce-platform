using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Identity.Commands.RevokeStateRepAssignment;

/// <summary>
/// Revokes (soft-deletes) the given state-rep assignment.
/// Returns <see cref="Result{Unit}"/> so the endpoint can map to HTTP 204.
/// </summary>
public sealed record RevokeStateRepAssignmentCommand(System.Guid Id) : IRequest<Result<CCE.Application.Common.Unit>>;
