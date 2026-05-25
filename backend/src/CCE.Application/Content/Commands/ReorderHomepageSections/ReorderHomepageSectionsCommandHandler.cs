using CCE.Application.Content;
using MediatR;

namespace CCE.Application.Content.Commands.ReorderHomepageSections;

public sealed class ReorderHomepageSectionsCommandHandler
    : IRequestHandler<ReorderHomepageSectionsCommand, Unit>
{
    private readonly IHomepageSectionRepository _service;

    public ReorderHomepageSectionsCommandHandler(IHomepageSectionRepository service)
    {
        _service = service;
    }

    public async Task<Unit> Handle(ReorderHomepageSectionsCommand request, CancellationToken cancellationToken)
    {
        var pairs = request.Assignments.Select(a => (a.Id, a.OrderIndex)).ToList();
        await _service.ReorderAsync(pairs, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
