using CCE.Application.Kapsarc.Dtos;
using MediatR;

namespace CCE.Application.Kapsarc.Queries.GetLatestKapsarcSnapshot;

public sealed record GetLatestKapsarcSnapshotQuery(System.Guid CountryId)
    : IRequest<KapsarcSnapshotDto?>;
