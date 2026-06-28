using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.PlatformSettings;
using CCE.Domain.PlatformSettings.ValueObjects;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.CreateGlossaryEntry;

public sealed class CreateGlossaryEntryCommandHandler
    : IRequestHandler<CreateGlossaryEntryCommand, Response<System.Guid>>
{
    private readonly IAboutSettingsRepository _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;

    public CreateGlossaryEntryCommandHandler(
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
        CreateGlossaryEntryCommand request, CancellationToken cancellationToken)
    {
        var about = await _repo.GetAsync(cancellationToken).ConfigureAwait(false);
        if (about is null)
            return _msg.NotFound<System.Guid>(MessageKeys.PlatformSettings.ABOUT_SETTINGS_NOT_FOUND);

        var userId = _currentUser.GetUserId()
            ?? throw new DomainException("User identity required.");
        var term = LocalizedText.Create(request.TermAr, request.TermEn);
        var definition = LocalizedText.Create(request.DefinitionAr, request.DefinitionEn);

        var entry = about.AddGlossaryEntry(term, definition, userId, _clock);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.Ok(entry.Id, MessageKeys.Content.CONTENT_CREATED);
    }
}
