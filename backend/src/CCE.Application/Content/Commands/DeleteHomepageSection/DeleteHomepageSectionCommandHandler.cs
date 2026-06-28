using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Content.Commands.DeleteHomepageSection;

public sealed class DeleteHomepageSectionCommandHandler : IRequestHandler<DeleteHomepageSectionCommand, Response<VoidData>>
{
    private readonly IHomepageSectionRepository _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;

    public DeleteHomepageSectionCommandHandler(IHomepageSectionRepository service, ICurrentUserAccessor currentUser, ISystemClock clock, MessageFactory msg)
    {
        _service = service;
        _currentUser = currentUser;
        _clock = clock;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(DeleteHomepageSectionCommand request, CancellationToken cancellationToken)
    {
        var section = await _service.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (section is null)
        {
            return _msg.NotFound<VoidData>(MessageKeys.PlatformSettings.HOMEPAGE_SECTION_NOT_FOUND);
        }

        var deletedById = _currentUser.GetUserId();
        if (deletedById is null)
        {
            return _msg.Unauthorized<VoidData>(MessageKeys.Identity.NOT_AUTHENTICATED);
        }

        section.SoftDelete(deletedById.Value, _clock);
        await _service.UpdateAsync(section, cancellationToken).ConfigureAwait(false);
        return _msg.Ok(MessageKeys.Content.CONTENT_DELETED);
    }
}
