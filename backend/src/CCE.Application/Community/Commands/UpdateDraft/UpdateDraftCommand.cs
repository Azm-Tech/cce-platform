using System.Collections.Generic;
using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Community.Commands.UpdateDraft;

/// <summary>Edits an unpublished draft (author only).</summary>
public sealed record UpdateDraftCommand(
    Guid PostId,
    string Title,
    string? Content,
    IReadOnlyList<Guid> TagIds) : IRequest<Response<VoidData>>;
