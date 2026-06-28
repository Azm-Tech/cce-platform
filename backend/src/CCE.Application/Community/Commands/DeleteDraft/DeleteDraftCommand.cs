using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Community.Commands.DeleteDraft;

/// <summary>Hard-deletes an unpublished draft (author only). Published posts use moderation.</summary>
public sealed record DeleteDraftCommand(Guid PostId) : IRequest<Response<VoidData>>;
