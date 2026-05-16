using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Identity.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Identity.Commands.RejectExpertRequest;

public sealed class RejectExpertRequestCommandHandler
    : IRequestHandler<RejectExpertRequestCommand, Response<ExpertRequestDto>>
{
    private readonly ICceDbContext _db;
    private readonly IExpertWorkflowRepository _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;

    public RejectExpertRequestCommandHandler(
        ICceDbContext db,
        IExpertWorkflowRepository service,
        ICurrentUserAccessor currentUser,
        ISystemClock clock,
        MessageFactory msg)
    {
        _db = db;
        _service = service;
        _currentUser = currentUser;
        _clock = clock;
        _msg = msg;
    }

    public async Task<Response<ExpertRequestDto>> Handle(
        RejectExpertRequestCommand request,
        CancellationToken cancellationToken)
    {
        var registration = await _service.FindIncludingDeletedAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (registration is null)
        {
            return _msg.NotFound<ExpertRequestDto>("EXPERT_REQUEST_NOT_FOUND");
        }

        var rejectedById = _currentUser.GetUserId();
        if (rejectedById is null)
        {
            return _msg.NotAuthenticated<ExpertRequestDto>();
        }

        registration.Reject(rejectedById.Value, request.RejectionReasonAr, request.RejectionReasonEn, _clock);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var userName = (await _db.Users.Where(u => u.Id == registration.RequestedById).Select(u => u.UserName)
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false)).FirstOrDefault();

        return _msg.Ok(new ExpertRequestDto(
            registration.Id,
            registration.RequestedById,
            userName,
            registration.RequestedBioAr,
            registration.RequestedBioEn,
            registration.RequestedTags.ToList(),
            registration.SubmittedOn,
            registration.Status,
            registration.ProcessedById,
            registration.ProcessedOn,
            registration.RejectionReasonAr,
            registration.RejectionReasonEn), "EXPERT_REQUEST_REJECTED");
    }
}
