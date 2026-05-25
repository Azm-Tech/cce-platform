using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.PlatformSettings;
using CCE.Domain.PlatformSettings.ValueObjects;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.UpdateKnowledgePartner;

public sealed class UpdateKnowledgePartnerCommandHandler
    : IRequestHandler<UpdateKnowledgePartnerCommand, Response<System.Guid>>
{
    private readonly IAboutSettingsRepository _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;

    public UpdateKnowledgePartnerCommandHandler(
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
        UpdateKnowledgePartnerCommand request, CancellationToken cancellationToken)
    {
        var about = await _repo.GetAsync(cancellationToken).ConfigureAwait(false);
        if (about is null)
            return _msg.AboutSettingsNotFound<System.Guid>();

        var partner = about.KnowledgePartners.FirstOrDefault(p => p.Id == request.Id);
        if (partner is null)
            return _msg.KnowledgePartnerNotFound<System.Guid>();

        var userId = _currentUser.GetUserId()
            ?? throw new DomainException("User identity required.");
        var name = LocalizedText.Create(request.NameAr, request.NameEn);
        LocalizedText? description = null;
        if (!string.IsNullOrWhiteSpace(request.DescriptionAr) && !string.IsNullOrWhiteSpace(request.DescriptionEn))
        {
            description = LocalizedText.Create(request.DescriptionAr, request.DescriptionEn);
        }

        about.UpdateKnowledgePartner(partner, name, description, request.LogoUrl, request.WebsiteUrl, userId, _clock);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.Ok(partner.Id, "CONTENT_UPDATED");
    }
}
