using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Identity.Dtos;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Identity.Commands.RejectExpertRequest;

public sealed class RejectExpertRequestCommandHandler
    : IRequestHandler<RejectExpertRequestCommand, Result<ExpertRequestDto>>
{
    private readonly IExpertWorkflowRepository _service;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly CCE.Application.Common.Errors _errors;

    public RejectExpertRequestCommandHandler(
        IExpertWorkflowRepository service,
        ICceDbContext db,
        ICurrentUserAccessor currentUser,
        ISystemClock clock,
        CCE.Application.Common.Errors errors)
    {
        _service = service;
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _errors = errors;
    }

    public async Task<Result<ExpertRequestDto>> Handle(
        RejectExpertRequestCommand request,
        CancellationToken cancellationToken)
    {
        var registration = await _service.FindIncludingDeletedAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (registration is null)
        {
            return _errors.ExpertRequestNotFound();
        }

        var rejectedById = _currentUser.GetUserId();
        if (rejectedById is null)
        {
            return _errors.NotAuthenticated();
        }

        registration.Reject(rejectedById.Value, request.RejectionReasonAr, request.RejectionReasonEn, _clock);
        await _service.SaveAsync(registration, newProfile: null, cancellationToken).ConfigureAwait(false);

        var userName = (await _db.Users.Where(u => u.Id == registration.RequestedById).Select(u => u.UserName)
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false)).FirstOrDefault();

        return new ExpertRequestDto(
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
            registration.RejectionReasonEn);
    }
}
