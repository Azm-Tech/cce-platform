using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.CommunityLaws;
using CCE.Domain.PlatformSettings.ValueObjects;
using MediatR;

namespace CCE.Application.CommunityLaws.Commands.UpdateCommunityLawSection;

internal sealed class UpdateCommunityLawSectionCommandHandler(
    IRepository<CommunityLawSection, Guid> _repo,
    ICceDbContext _db,
    ICurrentUserAccessor _currentUser,
    ISystemClock _clock,
    MessageFactory _msg)
    : IRequestHandler<UpdateCommunityLawSectionCommand, Response<VoidData>>
{
    public async Task<Response<VoidData>> Handle(UpdateCommunityLawSectionCommand cmd, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(cmd.Id, ct);
        if (entity is null)
            return _msg.NotFound<VoidData>(MessageKeys.CommunityLaws.SECTION_NOT_FOUND);

        var userId = _currentUser.GetUserId();
        if (userId is null)
            return _msg.Unauthorized<VoidData>(MessageKeys.Identity.NOT_AUTHENTICATED);

        var title = LocalizedText.Create(cmd.TitleAr, cmd.TitleEn);
        var content = LocalizedText.Create(cmd.ContentAr, cmd.ContentEn);

        entity.UpdateContent(title, content, userId.Value, _clock);
        await _db.SaveChangesAsync(ct);

        return _msg.Ok(MessageKeys.CommunityLaws.SECTION_UPDATED);
    }
}
