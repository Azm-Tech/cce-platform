using CCE.Application.Common;
using MediatR;

namespace CCE.Application.CommunityLaws.Commands.ReorderCommunityLawSection;

public sealed record ReorderCommunityLawSectionCommand(
    Guid Id,
    int OrderIndex) : IRequest<Response<VoidData>>;
