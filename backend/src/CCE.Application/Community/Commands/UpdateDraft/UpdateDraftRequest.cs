using System.Collections.Generic;

namespace CCE.Application.Community.Commands.UpdateDraft;

/// <summary>Request body for the update-draft endpoint (post id comes from the route).</summary>
public sealed record UpdateDraftRequest(string Title, string? Content, IReadOnlyList<Guid>? TagIds);
