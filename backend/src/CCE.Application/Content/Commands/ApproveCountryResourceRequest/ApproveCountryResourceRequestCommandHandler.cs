using CCE.Application.Common.Interfaces;
using CCE.Application.Content;
using CCE.Application.Content.Dtos;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Content.Commands.ApproveCountryResourceRequest;

public sealed class ApproveCountryResourceRequestCommandHandler
    : IRequestHandler<ApproveCountryResourceRequestCommand, CountryResourceRequestDto>
{
    private readonly ICountryResourceRequestRepository _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;

    public ApproveCountryResourceRequestCommandHandler(
        ICountryResourceRequestRepository service,
        ICurrentUserAccessor currentUser,
        ISystemClock clock)
    {
        _service = service;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<CountryResourceRequestDto> Handle(
        ApproveCountryResourceRequestCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _service.FindIncludingDeletedAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (entity is null)
        {
            throw new System.Collections.Generic.KeyNotFoundException($"Country resource request {request.Id} not found.");
        }

        var approvedById = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot approve from a request without a user identity.");

        entity.Approve(approvedById, request.AdminNotesAr, request.AdminNotesEn, _clock);
        await _service.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);

        return MapToDto(entity);
    }

    internal static CountryResourceRequestDto MapToDto(CCE.Domain.Country.CountryResourceRequest e) => new(
        e.Id,
        e.CountryId,
        e.RequestedById,
        e.Status,
        e.ProposedTitleAr,
        e.ProposedTitleEn,
        e.ProposedDescriptionAr,
        e.ProposedDescriptionEn,
        e.ProposedResourceType,
        e.ProposedAssetFileId,
        e.SubmittedOn,
        e.AdminNotesAr,
        e.AdminNotesEn,
        e.ProcessedById,
        e.ProcessedOn);
}
