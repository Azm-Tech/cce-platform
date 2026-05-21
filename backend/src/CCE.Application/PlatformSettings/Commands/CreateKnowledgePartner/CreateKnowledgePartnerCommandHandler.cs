using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Application.PlatformSettings.Dtos;
using CCE.Domain.PlatformSettings;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.CreateKnowledgePartner;

public sealed class CreateKnowledgePartnerCommandHandler
    : IRequestHandler<CreateKnowledgePartnerCommand, Response<KnowledgePartnerDto>>
{
    private readonly IAboutSettingsRepository _aboutRepo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public CreateKnowledgePartnerCommandHandler(
        IAboutSettingsRepository aboutRepo, ICceDbContext db, MessageFactory msg)
    {
        _aboutRepo = aboutRepo;
        _db = db;
        _msg = msg;
    }

    public async Task<Response<KnowledgePartnerDto>> Handle(
        CreateKnowledgePartnerCommand request, CancellationToken cancellationToken)
    {
        var about = await _aboutRepo.GetAsync(cancellationToken).ConfigureAwait(false);
        if (about is null)
            return _msg.AboutSettingsNotFound<KnowledgePartnerDto>();

        var maxOrder = await _db.KnowledgePartners
            .Where(p => p.AboutSettingsId == about.Id)
            .Select(p => (int?)p.OrderIndex)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var nextOrder = (maxOrder.FirstOrDefault() ?? -1) + 1;

        var partner = KnowledgePartner.Create(
            about.Id, request.NameAr, request.NameEn,
            request.LogoUrl, request.WebsiteUrl,
            request.DescriptionAr, request.DescriptionEn, nextOrder);

        _db.Add(partner);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.Ok(new KnowledgePartnerDto(
            partner.Id, partner.NameAr, partner.NameEn, partner.LogoUrl, partner.WebsiteUrl,
            partner.DescriptionAr, partner.DescriptionEn, partner.OrderIndex), "CONTENT_CREATED");
    }
}
