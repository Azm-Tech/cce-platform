using CCE.Application.Common.Interfaces;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Content.Commands.DeleteHomepageSection;

public sealed class DeleteHomepageSectionCommandHandler : IRequestHandler<DeleteHomepageSectionCommand, Unit>
{
    private readonly IHomepageSectionService _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;

    public DeleteHomepageSectionCommandHandler(IHomepageSectionService service, ICurrentUserAccessor currentUser, ISystemClock clock)
    {
        _service = service;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Unit> Handle(DeleteHomepageSectionCommand request, CancellationToken cancellationToken)
    {
        var section = await _service.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (section is null)
        {
            throw new System.Collections.Generic.KeyNotFoundException($"HomepageSection {request.Id} not found.");
        }

        var deletedById = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot delete homepage section from a request without a user identity.");

        section.SoftDelete(deletedById, _clock);
        await _service.UpdateAsync(section, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
