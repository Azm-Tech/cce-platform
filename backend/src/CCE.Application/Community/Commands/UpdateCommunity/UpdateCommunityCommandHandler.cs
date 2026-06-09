using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Errors;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Community.Commands.UpdateCommunity;

public sealed class UpdateCommunityCommandHandler
    : IRequestHandler<UpdateCommunityCommand, Response<VoidData>>
{
    private readonly ICommunityRepository _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public UpdateCommunityCommandHandler(ICommunityRepository repo, ICceDbContext db, MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(UpdateCommunityCommand request, CancellationToken cancellationToken)
    {
        var community = await _repo.GetAsync(request.CommunityId, cancellationToken).ConfigureAwait(false);
        if (community is null) return _msg.NotFound<VoidData>(ApplicationErrors.Community.COMMUNITY_NOT_FOUND);

        community.UpdateContent(request.NameAr, request.NameEn, request.DescriptionAr,
            request.DescriptionEn, request.PresentationJson);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return _msg.Ok(ApplicationErrors.General.SUCCESS_UPDATED);
    }
}
