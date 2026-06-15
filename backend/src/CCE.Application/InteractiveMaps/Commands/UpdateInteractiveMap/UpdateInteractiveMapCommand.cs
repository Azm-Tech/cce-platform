using CCE.Application.Common;
using MediatR;

namespace CCE.Application.InteractiveMaps.Commands.UpdateInteractiveMap;

public sealed record UpdateInteractiveMapCommand(
    System.Guid Id,
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    string Slug,
    bool IsActive) : IRequest<Response<VoidData>>;
