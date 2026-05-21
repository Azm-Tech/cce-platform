using CCE.Application.Common;
using CCE.Application.PlatformSettings.Dtos;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.UpdateGlossaryEntry;

public sealed record UpdateGlossaryEntryCommand(
    System.Guid Id,
    string TermAr,
    string TermEn,
    string DefinitionAr,
    string DefinitionEn) : IRequest<Response<GlossaryEntryDto>>;
