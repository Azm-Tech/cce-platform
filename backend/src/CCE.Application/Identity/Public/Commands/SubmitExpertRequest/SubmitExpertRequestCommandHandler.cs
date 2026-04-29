using CCE.Application.Identity.Public.Dtos;
using CCE.Domain.Common;
using CCE.Domain.Identity;
using MediatR;

namespace CCE.Application.Identity.Public.Commands.SubmitExpertRequest;

public sealed class SubmitExpertRequestCommandHandler
    : IRequestHandler<SubmitExpertRequestCommand, ExpertRequestStatusDto>
{
    private readonly IExpertRequestSubmissionService _service;
    private readonly ISystemClock _clock;

    public SubmitExpertRequestCommandHandler(IExpertRequestSubmissionService service, ISystemClock clock)
    {
        _service = service;
        _clock = clock;
    }

    public async Task<ExpertRequestStatusDto> Handle(SubmitExpertRequestCommand request, CancellationToken cancellationToken)
    {
        var entity = ExpertRegistrationRequest.Submit(
            request.RequesterId,
            request.RequestedBioAr,
            request.RequestedBioEn,
            request.RequestedTags,
            _clock);
        await _service.SaveAsync(entity, cancellationToken).ConfigureAwait(false);

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
