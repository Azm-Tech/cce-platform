using CCE.Application.Common;
using MediatR;

namespace CCE.Application.CommunityLaws.Commands.DeleteCommunityLawSection;

public sealed record DeleteCommunityLawSectionCommand(Guid Id) : IRequest<Response<VoidData>>;
