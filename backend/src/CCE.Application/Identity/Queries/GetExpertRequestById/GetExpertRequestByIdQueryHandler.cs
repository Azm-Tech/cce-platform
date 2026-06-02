using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Identity.Dtos;
using CCE.Application.Identity.Public.Dtos;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Identity.Queries.GetExpertRequestById;

public sealed class GetExpertRequestByIdQueryHandler
    : IRequestHandler<GetExpertRequestByIdQuery, Response<ExpertRequestDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetExpertRequestByIdQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<ExpertRequestDto>> Handle(
        GetExpertRequestByIdQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await _db.ExpertRegistrationRequests
            .Where(r => r.Id == request.Id)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        var row = rows.FirstOrDefault();
        if (row is null)
            return _msg.ExpertRequestNotFound<ExpertRequestDto>();

        var userNames = await _db.Users
            .Where(u => u.Id == row.RequestedById)
            .Select(u => u.UserName)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        var attachments = await _db.ExpertRequestAttachments
            .Where(a => a.ExpertRequestId == row.Id)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        var assetIds = attachments.Select(a => a.AssetFileId).ToList();
        var assetUrlMap = (await _db.AssetFiles
            .Where(a => assetIds.Contains(a.Id))
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false))
            .ToDictionary(a => a.Id, a => a.Url);

        return _msg.Ok(new ExpertRequestDto(
            row.Id,
            row.RequestedById,
            userNames.FirstOrDefault(),
            row.RequestedBioAr,
            row.RequestedBioEn,
            row.RequestedTags.ToList(),
            attachments.Select(a => new ExpertRequestAttachmentDto(
                a.Id, a.AssetFileId, a.AttachmentType, a.UploadedAt,
                assetUrlMap.GetValueOrDefault(a.AssetFileId) ?? string.Empty)).ToList(),
            row.SubmittedOn,
            row.Status,
            row.ProcessedById,
            row.ProcessedOn,
            row.RejectionReasonAr,
            row.RejectionReasonEn), "SUCCESS_OPERATION");
    }
}
