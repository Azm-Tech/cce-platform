using CCE.Application.Common.Interfaces;
using CCE.Application.Content;
using CCE.Application.Content.Commands.ApproveCountryResourceRequest;
using CCE.Application.Content.Dtos;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Content.Commands.RejectCountryResourceRequest;

public sealed class RejectCountryResourceRequestCommandHandler
    : IRequestHandler<RejectCountryResourceRequestCommand, CountryResourceRequestDto>
{
    private readonly ICountryResourceRequestService _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;

    public RejectCountryResourceRequestCommandHandler(
        ICountryResourceRequestService service,
        ICurrentUserAccessor currentUser,
        ISystemClock clock)
    {
        _service = service;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<CountryResourceRequestDto> Handle(
        RejectCountryResourceRequestCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _service.FindIncludingDeletedAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (entity is null)
        {
            throw new System.Collections.Generic.KeyNotFoundException($"Country resource request {request.Id} not found.");
        }

        var rejectedById = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot reject from a request without a user identity.");

        entity.Reject(rejectedById, request.AdminNotesAr, request.AdminNotesEn, _clock);
        await _service.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);

        return ApproveCountryResourceRequestCommandHandler.MapToDto(entity);
    }
}
