using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Identity.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.Domain.Identity;
using MediatR;

namespace CCE.Application.Identity.Public.Commands.SubmitExpertRequest;

public sealed class SubmitExpertRequestCommandHandler
    : IRequestHandler<SubmitExpertRequestCommand, Response<ExpertRequestStatusDto>>
{
    private readonly ICceDbContext _db;
    private readonly IExpertRequestSubmissionRepository _service;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;

    public SubmitExpertRequestCommandHandler(
        ICceDbContext db,
        IExpertRequestSubmissionRepository service,
        ISystemClock clock,
        MessageFactory msg)
    {
        _db = db;
        _service = service;
        _clock = clock;
        _msg = msg;
    }

    public async Task<Response<ExpertRequestStatusDto>> Handle(SubmitExpertRequestCommand request, CancellationToken cancellationToken)
    {
        // READ: validate CV asset via ICceDbContext directly
        var assets = await _db.AssetFiles
            .Where(a => a.Id == request.CvAssetFileId)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        var asset = assets.FirstOrDefault();
        if (asset is null)
            return _msg.NotFound<ExpertRequestStatusDto>(MessageKeys.Content.ASSET_NOT_FOUND);
        if (asset.VirusScanStatus != VirusScanStatus.Clean)
            return _msg.BusinessRule<ExpertRequestStatusDto>(MessageKeys.Content.ASSET_NOT_CLEAN);

        // WRITE: create aggregate via domain factory
        var entity = ExpertRegistrationRequest.Submit(
            request.RequesterId,
            request.RequestedBioAr,
            request.RequestedBioEn,
            request.RequestedTags,
            request.CvAssetFileId,
            _clock);

        // fetch-add via generic repository, save via ICceDbContext (unit of work)
        await _service.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.Ok(new ExpertRequestStatusDto(
            entity.Id,
            entity.RequestedById,
            entity.RequestedBioAr,
            entity.RequestedBioEn,
            entity.RequestedTags.ToList(),
            entity.Attachments.Select(a => new ExpertRequestAttachmentDto(a.Id, a.AssetFileId, a.AttachmentType, a.UploadedAt)).ToList(),
            entity.SubmittedOn,
            entity.Status,
            entity.ProcessedOn,
            entity.RejectionReasonAr,
            entity.RejectionReasonEn), MessageKeys.Identity.EXPERT_REQUEST_SUBMITTED);
    }
}
