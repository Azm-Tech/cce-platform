using CCE.Application.Common;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.CreateGlossaryEntry;

public sealed record CreateGlossaryEntryCommand(
    string TermAr,
    string TermEn,
    string DefinitionAr,
    string DefinitionEn) : IRequest<Response<System.Guid>>;
