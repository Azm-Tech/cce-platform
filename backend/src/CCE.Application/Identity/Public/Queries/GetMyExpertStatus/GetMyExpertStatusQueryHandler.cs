using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Identity.Public.Dtos;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Identity.Public.Queries.GetMyExpertStatus;

public sealed class GetMyExpertStatusQueryHandler : IRequestHandler<GetMyExpertStatusQuery, Response<ExpertRequestStatusDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetMyExpertStatusQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<ExpertRequestStatusDto>> Handle(GetMyExpertStatusQuery request, CancellationToken cancellationToken)
    {
        var rows = await _db.ExpertRegistrationRequests
            .Where(r => r.RequestedById == request.UserId)
            .OrderByDescending(r => r.SubmittedOn)
            .Take(1)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        var entity = rows.FirstOrDefault();
        if (entity is null)
        {
            return _msg.NotFound<ExpertRequestStatusDto>("EXPERT_REQUEST_NOT_FOUND");
        }

        return _msg.Ok(new ExpertRequestStatusDto(
            entity.Id,
            entity.RequestedById,
            entity.RequestedBioAr,
            entity.RequestedBioEn,
            entity.RequestedTags.ToList(),
            entity.SubmittedOn,
            entity.Status,
            entity.ProcessedOn,
            entity.RejectionReasonAr,
            entity.RejectionReasonEn), "SUCCESS_OPERATION");
    }
}
