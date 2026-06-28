using CCE.Application.Common;
using CCE.Application.Kapsarc.Dtos;
using MediatR;

namespace CCE.Application.Kapsarc.Commands.RefreshKapsarcSnapshot;

/// <summary>
/// Pulls the latest Circular Carbon Economy data for a country from KAPSARC
/// (US014 / BRD §6.5.1), captures it as a new snapshot and updates the
/// country's latest-snapshot pointer.
/// </summary>
public sealed record RefreshKapsarcSnapshotCommand(System.Guid CountryId)
    : IRequest<Response<KapsarcSnapshotDto>>;
