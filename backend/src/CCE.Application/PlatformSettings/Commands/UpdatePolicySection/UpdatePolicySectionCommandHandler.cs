using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Application.PlatformSettings.Dtos;
using CCE.Domain.PlatformSettings;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.UpdatePolicySection;

public sealed class UpdatePolicySectionCommandHandler
    : IRequestHandler<UpdatePolicySectionCommand, Response<PolicySectionDto>>
{
    private readonly IPolicySectionRepository _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public UpdatePolicySectionCommandHandler(
        IPolicySectionRepository repo, ICceDbContext db, MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _msg = msg;
    }

    public async Task<Response<PolicySectionDto>> Handle(
        UpdatePolicySectionCommand request, CancellationToken cancellationToken)
    {
        var section = await _repo.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (section is null)
            return _msg.PolicySectionNotFound<PolicySectionDto>();

        section.UpdateContent(
            request.TitleAr, request.TitleEn,
            request.ContentAr, request.ContentEn);

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.Ok(new PolicySectionDto(
            section.Id, (int)section.Type, section.TitleAr, section.TitleEn,
            section.ContentAr, section.ContentEn, section.OrderIndex), "CONTENT_UPDATED");
    }
}
