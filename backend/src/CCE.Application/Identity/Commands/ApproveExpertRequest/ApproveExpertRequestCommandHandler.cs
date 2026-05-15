using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Identity.Dtos;
using CCE.Domain.Common;
using CCE.Domain.Identity;
using MediatR;

namespace CCE.Application.Identity.Commands.ApproveExpertRequest;

public sealed class ApproveExpertRequestCommandHandler
    : IRequestHandler<ApproveExpertRequestCommand, Result<ExpertProfileDto>>
{
    private readonly IExpertWorkflowRepository _service;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly CCE.Application.Common.Errors _errors;

    public ApproveExpertRequestCommandHandler(
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

    public async Task<Result<ExpertProfileDto>> Handle(
        ApproveExpertRequestCommand request,
        CancellationToken cancellationToken)
    {
        var registration = await _service.FindIncludingDeletedAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (registration is null)
        {
            return _errors.ExpertRequestNotFound();
        }

        var approvedById = _currentUser.GetUserId();
        if (approvedById is null)
        {
            return _errors.NotAuthenticated();
        }

        registration.Approve(approvedById.Value, _clock);
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
