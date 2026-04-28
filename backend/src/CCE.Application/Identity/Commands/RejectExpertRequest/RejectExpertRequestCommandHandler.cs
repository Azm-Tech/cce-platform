using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Identity;
using CCE.Application.Identity.Dtos;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Identity.Commands.RejectExpertRequest;

public sealed class RejectExpertRequestCommandHandler
    : IRequestHandler<RejectExpertRequestCommand, ExpertRequestDto>
{
    private readonly IExpertWorkflowService _service;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;

    public RejectExpertRequestCommandHandler(
        IExpertWorkflowService service,
        ICceDbContext db,
        ICurrentUserAccessor currentUser,
        ISystemClock clock)
    {
        _service = service;
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<ExpertRequestDto> Handle(
        RejectExpertRequestCommand request,
        CancellationToken cancellationToken)
    {
        var registration = await _service.FindIncludingDeletedAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (registration is null)
        {
            throw new System.Collections.Generic.KeyNotFoundException($"Expert registration request {request.Id} not found.");
        }

        var rejectedById = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot reject an expert request from a request without a user identity.");

        registration.Reject(rejectedById, request.RejectionReasonAr, request.RejectionReasonEn, _clock);
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
