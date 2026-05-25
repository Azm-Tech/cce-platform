using CCE.Application.Common;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.DeleteGlossaryEntry;

public sealed record DeleteGlossaryEntryCommand(System.Guid Id) : IRequest<Response<VoidData>>;
