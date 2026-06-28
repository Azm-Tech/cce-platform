using CCE.Application.Common;
using MediatR;

namespace CCE.Application.InteractiveMaps.Commands.CreateInteractiveMap;

public sealed record CreateInteractiveMapCommand(
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn) : IRequest<Response<VoidData>>;
