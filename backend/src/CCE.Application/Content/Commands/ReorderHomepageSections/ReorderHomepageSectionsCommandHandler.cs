using CCE.Application.Common;
using CCE.Application.Content;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Content.Commands.ReorderHomepageSections;

public sealed class ReorderHomepageSectionsCommandHandler
    : IRequestHandler<ReorderHomepageSectionsCommand, Response<VoidData>>
{
    private readonly IHomepageSectionRepository _service;
    private readonly MessageFactory _msg;

    public ReorderHomepageSectionsCommandHandler(IHomepageSectionRepository service, MessageFactory msg)
    {
        _service = service;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(ReorderHomepageSectionsCommand request, CancellationToken cancellationToken)
    {
        var pairs = request.Assignments.Select(a => (a.Id, a.OrderIndex)).ToList();
        await _service.ReorderAsync(pairs, cancellationToken).ConfigureAwait(false);
        return _msg.Ok(MessageKeys.General.SUCCESS_UPDATED);
    }
}
