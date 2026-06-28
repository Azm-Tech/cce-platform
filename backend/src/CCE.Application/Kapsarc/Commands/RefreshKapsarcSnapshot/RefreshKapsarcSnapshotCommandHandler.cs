using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Kapsarc.Dtos;
using CCE.Application.Kapsarc.Queries.GetLatestKapsarcSnapshot;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Country;
using MediatR;

namespace CCE.Application.Kapsarc.Commands.RefreshKapsarcSnapshot;

public sealed class RefreshKapsarcSnapshotCommandHandler
    : IRequestHandler<RefreshKapsarcSnapshotCommand, Response<KapsarcSnapshotDto>>
{
    private readonly IRepository<CCE.Domain.Country.Country, System.Guid> _countries;
    private readonly IRepository<CountryKapsarcSnapshot, System.Guid> _snapshots;
    private readonly ICceDbContext _db;
    private readonly IKapsarcClient _kapsarc;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _messages;

    public RefreshKapsarcSnapshotCommandHandler(
        IRepository<CCE.Domain.Country.Country, System.Guid> countries,
        IRepository<CountryKapsarcSnapshot, System.Guid> snapshots,
        ICceDbContext db,
        IKapsarcClient kapsarc,
        ISystemClock clock,
        MessageFactory messages)
    {
        _countries = countries;
        _snapshots = snapshots;
        _db = db;
        _kapsarc = kapsarc;
        _clock = clock;
        _messages = messages;
    }

    public async Task<Response<KapsarcSnapshotDto>> Handle(
        RefreshKapsarcSnapshotCommand request,
        CancellationToken cancellationToken)
    {
        var country = await _countries.GetByIdAsync(request.CountryId, cancellationToken).ConfigureAwait(false);
        if (country is null)
            return _messages.NotFound<KapsarcSnapshotDto>(MessageKeys.Country.COUNTRY_NOT_FOUND);

        // Live retrieval from KAPSARC (inputs per BRD §6.5.1: ISO code + country name)
        var result = await _kapsarc
            .GetClassificationAsync(country.IsoAlpha3!, country.NameEn, cancellationToken)
            .ConfigureAwait(false);

        if (!result.Success || result.Classification is null
            || result.PerformanceScore is null || result.TotalIndex is null)
        {
            // BRD ER001 — no KAPSARC output / data unavailable
            return _messages.BusinessRule<KapsarcSnapshotDto>(MessageKeys.Country.KAPSARC_DATA_UNAVAILABLE);
        }

        CountryKapsarcSnapshot snapshot;
        try
        {
            snapshot = CountryKapsarcSnapshot.Capture(
                country.Id,
                result.Classification,
                result.PerformanceScore.Value,
                result.TotalIndex.Value,
                _clock,
                sourceVersion: "KAPSARC");
        }
        catch (DomainException)
        {
            // Out-of-range / invalid payload from the gateway → treat as unavailable
            return _messages.BusinessRule<KapsarcSnapshotDto>(MessageKeys.Country.KAPSARC_DATA_UNAVAILABLE);
        }

        await _snapshots.AddAsync(snapshot, cancellationToken).ConfigureAwait(false);
        country.UpdateLatestKapsarcSnapshot(snapshot.Id);

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _messages.Ok(
            GetLatestKapsarcSnapshotQueryHandler.MapToDto(snapshot), MessageKeys.Country.KAPSARC_SNAPSHOT_REFRESHED);
    }
}
