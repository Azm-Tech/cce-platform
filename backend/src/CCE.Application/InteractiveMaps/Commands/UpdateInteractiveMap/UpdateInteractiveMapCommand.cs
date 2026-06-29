using CCE.Application.Common;
using MediatR;

namespace CCE.Application.InteractiveMaps.Commands.UpdateInteractiveMap;

public sealed record UpdateInteractiveMapCommand(
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn) : IRequest<Response<VoidData>>;
