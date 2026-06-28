using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.CommunityLaws;
using MediatR;

namespace CCE.Application.CommunityLaws.Commands.DeleteCommunityLawSection;

internal sealed class DeleteCommunityLawSectionCommandHandler(
    IRepository<CommunityLawSection, Guid> _repo,
    ICceDbContext _db,
    MessageFactory _msg)
    : IRequestHandler<DeleteCommunityLawSectionCommand, Response<VoidData>>
{
    public async Task<Response<VoidData>> Handle(DeleteCommunityLawSectionCommand cmd, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(cmd.Id, ct);
        if (entity is null)
            return _msg.NotFound<VoidData>(MessageKeys.CommunityLaws.SECTION_NOT_FOUND);

        _repo.Delete(entity);
        await _db.SaveChangesAsync(ct);

        return _msg.Ok(MessageKeys.CommunityLaws.SECTION_DELETED);
    }
}
