using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.PlatformSettings;
using CCE.Domain.PlatformSettings.ValueObjects;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.CreatePolicySection;

public sealed class CreatePolicySectionCommandHandler
    : IRequestHandler<CreatePolicySectionCommand, Response<System.Guid>>
{
    private readonly IPoliciesSettingsRepository _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;

    public CreatePolicySectionCommandHandler(
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
        CreatePolicySectionCommand request, CancellationToken cancellationToken)
    {
        var settings = await _repo.GetAsync(cancellationToken).ConfigureAwait(false);
        if (settings is null)
            return _msg.NotFound<System.Guid>(MessageKeys.PlatformSettings.POLICIES_SETTINGS_NOT_FOUND);

        var userId = _currentUser.GetUserId()
            ?? throw new DomainException("User identity required.");
        var title = LocalizedText.Create(request.TitleAr, request.TitleEn);
        var content = LocalizedText.Create(request.ContentAr, request.ContentEn);
        var type = (PolicySectionType)request.Type;

        var section = settings.AddSection(type, title, content, userId, _clock);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.Ok(section.Id, MessageKeys.Content.CONTENT_CREATED);
    }
}
