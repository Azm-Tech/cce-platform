using MediatR;

namespace CCE.Application.Content.Commands.ReorderHomepageSections;

public sealed record ReorderHomepageSectionsCommand(
    System.Collections.Generic.IReadOnlyList<HomepageSectionOrderAssignment> Assignments)
    : IRequest<Unit>;

public sealed record HomepageSectionOrderAssignment(System.Guid Id, int OrderIndex);
