using CCE.Application.Common;
using CCE.Application.Media.Dtos;
using MediatR;

namespace CCE.Application.Media.Queries.GetMediaById;

public sealed record GetMediaByIdQuery(System.Guid Id) : IRequest<Response<MediaFileDto>>;
