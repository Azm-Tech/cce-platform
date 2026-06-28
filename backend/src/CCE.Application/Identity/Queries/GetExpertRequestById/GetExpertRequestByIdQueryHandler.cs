using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Identity.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Identity;
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
            return _msg.NotFound<ExpertRequestDto>(MessageKeys.Identity.EXPERT_REQUEST_NOT_FOUND);

        var userNames = await _db.Users
            .Where(u => u.Id == row.RequestedById)
            .Select(u => u.UserName)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        var cvAssetFileIds = await _db.ExpertRequestAttachments
            .Where(a => a.ExpertRequestId == row.Id && a.AttachmentType == ExpertRequestAttachmentType.Cv)
            .Select(a => (System.Guid?)a.AssetFileId)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        return _msg.Ok(new ExpertRequestDto(
            row.Id,
            row.RequestedById,
            userNames.FirstOrDefault(),
            row.RequestedBioAr,
            row.RequestedBioEn,
            row.RequestedTags.ToList(),
            row.SubmittedOn,
            row.Status,
            row.ProcessedById,
            row.ProcessedOn,
            row.RejectionReasonAr,
            row.RejectionReasonEn,
            cvAssetFileIds.FirstOrDefault()), MessageKeys.General.SUCCESS_OPERATION);
    }
}
