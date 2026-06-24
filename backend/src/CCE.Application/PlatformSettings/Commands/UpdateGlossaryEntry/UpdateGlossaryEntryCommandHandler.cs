using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.PlatformSettings;
using CCE.Domain.PlatformSettings.ValueObjects;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.UpdateGlossaryEntry;

public sealed class UpdateGlossaryEntryCommandHandler
    : IRequestHandler<UpdateGlossaryEntryCommand, Response<System.Guid>>
{
    private readonly IAboutSettingsRepository _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;

    public UpdateGlossaryEntryCommandHandler(
        IAboutSettingsRepository repo,
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
        UpdateGlossaryEntryCommand request, CancellationToken cancellationToken)
    {
        var about = await _repo.GetAsync(cancellationToken).ConfigureAwait(false);
        if (about is null)
            return _msg.AboutSettingsNotFound<System.Guid>();

        var entry = about.GlossaryEntries.FirstOrDefault(e => e.Id == request.Id);
        if (entry is null)
            return _msg.GlossaryEntryNotFound<System.Guid>();

        var userId = _currentUser.GetUserId()
            ?? throw new DomainException("User identity required.");
        var term = LocalizedText.Create(request.TermAr, request.TermEn);
        var definition = LocalizedText.Create(request.DefinitionAr, request.DefinitionEn);

        about.UpdateGlossaryEntry(entry, term, definition, userId, _clock);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.Ok(entry.Id, MessageKeys.Content.CONTENT_UPDATED);
    }
}
