using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Application.PlatformSettings.Dtos;
using CCE.Domain.PlatformSettings;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.UpdateKnowledgePartner;

public sealed class UpdateKnowledgePartnerCommandHandler
    : IRequestHandler<UpdateKnowledgePartnerCommand, Response<KnowledgePartnerDto>>
{
    private readonly IKnowledgePartnerRepository _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public UpdateKnowledgePartnerCommandHandler(
        IKnowledgePartnerRepository repo, ICceDbContext db, MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _msg = msg;
    }

    public async Task<Response<KnowledgePartnerDto>> Handle(
        UpdateKnowledgePartnerCommand request, CancellationToken cancellationToken)
    {
        var partner = await _repo.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (partner is null)
            return _msg.KnowledgePartnerNotFound<KnowledgePartnerDto>();

        partner.UpdateContent(
            request.NameAr, request.NameEn, request.LogoUrl,
            request.WebsiteUrl, request.DescriptionAr, request.DescriptionEn);

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.Ok(new KnowledgePartnerDto(
            partner.Id, partner.NameAr, partner.NameEn, partner.LogoUrl, partner.WebsiteUrl,
            partner.DescriptionAr, partner.DescriptionEn, partner.OrderIndex), "CONTENT_UPDATED");
    }
}
