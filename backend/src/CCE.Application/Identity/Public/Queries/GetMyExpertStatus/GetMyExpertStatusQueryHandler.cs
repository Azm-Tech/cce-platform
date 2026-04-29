using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Identity.Public.Dtos;
using MediatR;

namespace CCE.Application.Identity.Public.Queries.GetMyExpertStatus;

public sealed class GetMyExpertStatusQueryHandler : IRequestHandler<GetMyExpertStatusQuery, ExpertRequestStatusDto?>
{
    private readonly ICceDbContext _db;

    public GetMyExpertStatusQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<ExpertRequestStatusDto?> Handle(GetMyExpertStatusQuery request, CancellationToken cancellationToken)
    {
        var userId = request.UserId;
        var rows = await _db.ExpertRegistrationRequests
            .Where(r => r.RequestedById == userId)
            .OrderByDescending(r => r.SubmittedOn)
            .Take(1)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        var entity = rows.FirstOrDefault();
        if (entity is null)
        {
            return null;
        }

        return new ExpertRequestStatusDto(
            entity.Id,
            entity.RequestedById,
            entity.RequestedBioAr,
            entity.RequestedBioEn,
            entity.RequestedTags.ToList(),
            entity.SubmittedOn,
            entity.Status,
            entity.ProcessedOn,
            entity.RejectionReasonAr,
            entity.RejectionReasonEn);
    }
}
