using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.CommunityLaws;
using CCE.Domain.PlatformSettings.ValueObjects;
using MediatR;

namespace CCE.Application.CommunityLaws.Commands.CreateCommunityLawSection;

internal sealed class CreateCommunityLawSectionCommandHandler(
    IRepository<CommunityLawSection, Guid> _repo,
    ICceDbContext _db,
    ICurrentUserAccessor _currentUser,
    ISystemClock _clock,
    MessageFactory _msg)
    : IRequestHandler<CreateCommunityLawSectionCommand, Response<VoidData>>
{
    public async Task<Response<VoidData>> Handle(CreateCommunityLawSectionCommand cmd, CancellationToken ct)
    {
        var userId = _currentUser.GetUserId();
        if (userId is null)
            return _msg.Unauthorized<VoidData>(MessageKeys.Identity.NOT_AUTHENTICATED);

        var existingOrders = await _db.CommunityLawSections
            .Select(s => s.OrderIndex)
            .ToListAsyncEither(ct);
        var maxOrder = existingOrders.DefaultIfEmpty(0).Max() + 1;

        var title = LocalizedText.Create(cmd.TitleAr, cmd.TitleEn);
        var content = LocalizedText.Create(cmd.ContentAr, cmd.ContentEn);

        var entity = CommunityLawSection.Create(
            title, content, maxOrder, userId.Value, _clock);

        await _repo.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);

        return _msg.Ok(MessageKeys.CommunityLaws.SECTION_CREATED);
    }
}
