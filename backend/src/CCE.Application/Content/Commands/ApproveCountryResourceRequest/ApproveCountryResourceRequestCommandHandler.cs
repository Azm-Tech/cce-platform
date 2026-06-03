using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Country;
using MediatR;

namespace CCE.Application.Content.Commands.ApproveCountryResourceRequest;

public sealed class ApproveCountryResourceRequestCommandHandler
    : IRequestHandler<ApproveCountryResourceRequestCommand, Response<CountryContentRequestDto>>
{
    private readonly IRepository<CountryContentRequest, System.Guid> _repo;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _messages;

    public ApproveCountryResourceRequestCommandHandler(
        IRepository<CountryContentRequest, System.Guid> repo,
        ICceDbContext db,
        ICurrentUserAccessor currentUser,
        ISystemClock clock,
        MessageFactory messages)
    {
        _repo = repo;
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _messages = messages;
    }

    public async Task<Response<CountryContentRequestDto>> Handle(
        ApproveCountryResourceRequestCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return _messages.CountryContentRequestNotFound<CountryContentRequestDto>();

        var approvedById = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot approve from a request without a user identity.");

        try
        {
            entity.Approve(approvedById, request.AdminNotesAr, request.AdminNotesEn, _clock);
        }
        catch (DomainException)
        {
            // ERR031 — request is not in Pending state (already approved or rejected)
            return _messages.CountryRequestProcessingFailed<CountryContentRequestDto>();
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // CON023
        return _messages.CountryRequestProcessed(MapToDto(entity));
    }

    internal static CountryContentRequestDto MapToDto(CountryContentRequest e) => new(
        e.Id, e.CountryId, e.RequestedById, e.Kind, e.Status,
        e.ProposedTitleAr, e.ProposedTitleEn,
        e.ProposedDescriptionAr, e.ProposedDescriptionEn,
        e.ProposedResourceType, e.ProposedAssetFileId,
        e.ProposedTopicId, e.ProposedStartsOn, e.ProposedEndsOn,
        e.ProposedLocationAr, e.ProposedLocationEn, e.ProposedOnlineMeetingUrl,
        e.SubmittedOn, e.AdminNotesAr, e.AdminNotesEn,
        e.ProcessedById, e.ProcessedOn);
}
