using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Identity;
using CCE.Application.Identity.Dtos;
using CCE.Domain.Common;
using CCE.Domain.Identity;
using MediatR;

namespace CCE.Application.Identity.Commands.ApproveExpertRequest;

public sealed class ApproveExpertRequestCommandHandler
    : IRequestHandler<ApproveExpertRequestCommand, ExpertProfileDto>
{
    private readonly IExpertWorkflowService _service;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;

    public ApproveExpertRequestCommandHandler(
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

    public async Task<ExpertProfileDto> Handle(
        ApproveExpertRequestCommand request,
        CancellationToken cancellationToken)
    {
        var registration = await _service.FindIncludingDeletedAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (registration is null)
        {
            throw new System.Collections.Generic.KeyNotFoundException($"Expert registration request {request.Id} not found.");
        }

        var approvedById = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot approve an expert request from a request without a user identity.");

        registration.Approve(approvedById, _clock);
        var profile = ExpertProfile.CreateFromApprovedRequest(registration, request.AcademicTitleAr, request.AcademicTitleEn, _clock);
        await _service.SaveAsync(registration, profile, cancellationToken).ConfigureAwait(false);

        var userName = (await _db.Users.Where(u => u.Id == registration.RequestedById).Select(u => u.UserName)
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false)).FirstOrDefault();

        return new ExpertProfileDto(
            profile.Id,
            profile.UserId,
            userName,
            profile.BioAr,
            profile.BioEn,
            profile.ExpertiseTags.ToList(),
            profile.AcademicTitleAr,
            profile.AcademicTitleEn,
            profile.ApprovedOn,
            profile.ApprovedById);
    }
}
