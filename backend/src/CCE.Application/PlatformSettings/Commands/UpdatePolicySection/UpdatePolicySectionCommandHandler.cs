using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.PlatformSettings;
using CCE.Domain.PlatformSettings.ValueObjects;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.UpdatePolicySection;

public sealed class UpdatePolicySectionCommandHandler
    : IRequestHandler<UpdatePolicySectionCommand, Response<System.Guid>>
{
    private readonly IPoliciesSettingsRepository _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;

    public UpdatePolicySectionCommandHandler(
        IPoliciesSettingsRepository repo,
        ICceDbContext db,
        MessageFactory msg,
        ICurrentUserAccessor currentUser,
        ISystemClock clock)
    {
        _repo = repo;
        _db = db;
        _msg = msg;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Response<System.Guid>> Handle(
        UpdatePolicySectionCommand request, CancellationToken cancellationToken)
    {
        var settings = await _repo.GetAsync(cancellationToken).ConfigureAwait(false);
        if (settings is null)
            return _msg.PoliciesSettingsNotFound<System.Guid>();

        var section = settings.Sections.FirstOrDefault(s => s.Id == request.Id);
        if (section is null)
            return _msg.PolicySectionNotFound<System.Guid>();

        var userId = _currentUser.GetUserId()
            ?? throw new DomainException("User identity required.");
        var title = LocalizedText.Create(request.TitleAr, request.TitleEn);
        var content = LocalizedText.Create(request.ContentAr, request.ContentEn);

        settings.UpdateSection(section, title, content, userId, _clock);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.Ok(section.Id, MessageKeys.Content.CONTENT_UPDATED);
    }
}
