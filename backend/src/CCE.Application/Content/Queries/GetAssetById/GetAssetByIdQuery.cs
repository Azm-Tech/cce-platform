using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Queries.GetAssetById;

public sealed record GetAssetByIdQuery(System.Guid Id) : IRequest<AssetFileDto?>;
