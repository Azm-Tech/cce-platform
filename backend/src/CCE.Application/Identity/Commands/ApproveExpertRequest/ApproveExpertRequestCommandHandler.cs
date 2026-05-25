using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Identity.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Identity;
using MediatR;

namespace CCE.Application.Identity.Commands.ApproveExpertRequest;

public sealed class ApproveExpertRequestCommandHandler
    : IRequestHandler<ApproveExpertRequestCommand, Response<ExpertProfileDto>>
{
    private readonly ICceDbContext _db;
    private readonly IExpertWorkflowRepository _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;

    public ApproveExpertRequestCommandHandler(
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

    public async Task<Response<ExpertProfileDto>> Handle(
        ApproveExpertRequestCommand request,
        CancellationToken cancellationToken)
    {
        var registration = await _service.FindIncludingDeletedAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (registration is null)
        {
            return _msg.NotFound<ExpertProfileDto>("EXPERT_REQUEST_NOT_FOUND");
        }

        var approvedById = _currentUser.GetUserId();
        if (approvedById is null)
        {
            return _msg.NotAuthenticated<ExpertProfileDto>();
        }

        registration.Approve(approvedById.Value, _clock);
        var profile = ExpertProfile.CreateFromApprovedRequest(registration, request.AcademicTitleAr, request.AcademicTitleEn, _clock);
        _service.AddProfile(profile);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var userName = (await _db.Users.Where(u => u.Id == registration.RequestedById).Select(u => u.UserName)
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false)).FirstOrDefault();

        return _msg.Ok(new ExpertProfileDto(
            profile.Id,
            profile.UserId,
            userName,
            profile.BioAr,
            profile.BioEn,
            profile.ExpertiseTags.ToList(),
            profile.AcademicTitleAr,
            profile.AcademicTitleEn,
            profile.ApprovedOn,
            profile.ApprovedById), "EXPERT_REQUEST_APPROVED");
    }
}
