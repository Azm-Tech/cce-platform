using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Identity.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Common;
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
        var entity = ExpertRegistrationRequest.Submit(
            request.RequesterId,
            request.RequestedBioAr,
            request.RequestedBioEn,
            request.RequestedTags,
            _clock);
        await _service.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

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
            entity.RejectionReasonEn), "EXPERT_REQUEST_SUBMITTED");
    }
}
