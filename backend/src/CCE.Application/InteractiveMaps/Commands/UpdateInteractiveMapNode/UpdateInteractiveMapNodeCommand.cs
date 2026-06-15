using CCE.Application.Common;
using MediatR;

namespace CCE.Application.InteractiveMaps.Commands.UpdateInteractiveMapNode;

public sealed record UpdateInteractiveMapNodeCommand(
    System.Guid MapId,
    System.Guid Id,
    string NameAr,
    string NameEn,
    string IconKey,
    int? Category,
    string? CategoryNameAr,
    string? CategoryNameEn,
    int Level,
    System.Guid? ParentId,
    System.Guid? TopicId,
    string? TopicSlug,
    bool IsActive) : IRequest<Response<VoidData>>;
