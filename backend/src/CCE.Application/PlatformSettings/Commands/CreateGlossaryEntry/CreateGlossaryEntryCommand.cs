using CCE.Application.Common;
using CCE.Application.PlatformSettings.Dtos;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.CreateGlossaryEntry;

public sealed record CreateGlossaryEntryCommand(
    string TermAr,
    string TermEn,
    string DefinitionAr,
    string DefinitionEn) : IRequest<Response<GlossaryEntryDto>>;
