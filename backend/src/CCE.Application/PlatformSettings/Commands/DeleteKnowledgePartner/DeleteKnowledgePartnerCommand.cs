using CCE.Application.Common;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.DeleteKnowledgePartner;

public sealed record DeleteKnowledgePartnerCommand(System.Guid Id) : IRequest<Response<VoidData>>;
