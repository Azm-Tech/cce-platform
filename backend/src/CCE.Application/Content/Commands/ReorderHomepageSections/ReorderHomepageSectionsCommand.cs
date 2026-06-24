using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Content.Commands.ReorderHomepageSections;

public sealed record ReorderHomepageSectionsCommand(
    System.Collections.Generic.IReadOnlyList<HomepageSectionOrderAssignment> Assignments)
    : IRequest<Response<VoidData>>;

public sealed record HomepageSectionOrderAssignment(System.Guid Id, int OrderIndex);
