using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Commands.ApproveCountryResourceRequest;
using CCE.Application.Content.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Country;
using MediatR;

namespace CCE.Application.Content.Commands.RejectCountryResourceRequest;

public sealed class RejectCountryResourceRequestCommandHandler
    : IRequestHandler<RejectCountryResourceRequestCommand, Response<CountryContentRequestDto>>
{
    private readonly IRepository<CountryContentRequest, System.Guid> _repo;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _messages;

    public RejectCountryResourceRequestCommandHandler(
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
        RejectCountryResourceRequestCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return _messages.NotFound<CountryContentRequestDto>(MessageKeys.Content.COUNTRY_RESOURCE_REQUEST_NOT_FOUND);

        var rejectedById = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot reject from a request without a user identity.");

        try
        {
            entity.Reject(rejectedById, request.AdminNotesAr, request.AdminNotesEn, _clock);
        }
        catch (DomainException)
        {
            // ERR031 — request is not in Pending state (already approved or rejected)
            return _messages.BusinessRule<CountryContentRequestDto>(MessageKeys.Content.COUNTRY_REQUEST_PROCESSING_FAILED);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // CON023
        return _messages.Ok(ApproveCountryResourceRequestCommandHandler.MapToDto(entity), MessageKeys.Content.COUNTRY_REQUEST_PROCESSED);
    }
}
