using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.CommunityLaws;
using MediatR;

namespace CCE.Application.CommunityLaws.Commands.ReorderCommunityLawSection;

internal sealed class ReorderCommunityLawSectionCommandHandler(
    IRepository<CommunityLawSection, Guid> _repo,
    ICceDbContext _db,
    MessageFactory _msg)
    : IRequestHandler<ReorderCommunityLawSectionCommand, Response<VoidData>>
{
    public async Task<Response<VoidData>> Handle(ReorderCommunityLawSectionCommand cmd, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(cmd.Id, ct);
        if (entity is null)
            return _msg.NotFound<VoidData>(MessageKeys.CommunityLaws.SECTION_NOT_FOUND);

        entity.Reorder(cmd.OrderIndex);
        await _db.SaveChangesAsync(ct);

        return _msg.Ok(MessageKeys.CommunityLaws.CONTENT_REORDERED);
    }
}
