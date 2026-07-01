using CCE.Application.Common;
using MediatR;

namespace CCE.Application.InteractiveMaps.Commands.CreateInteractiveMapNode;

public sealed record CreateInteractiveMapNodeCommand(
    System.Guid InteractiveMapId,
    string NameAr,
    string NameEn,
    string IconKey,
    int? Category,
    string? CategoryNameAr,
    string? CategoryNameEn,
    string? TitleAr,
    string? TitleEn,
    string? DescriptionAr,
    string? DescriptionEn,
    System.Guid? ParentId,
    System.Guid TopicId) : IRequest<Response<VoidData>>;
