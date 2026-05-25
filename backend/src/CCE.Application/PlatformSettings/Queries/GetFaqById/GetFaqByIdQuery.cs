using CCE.Application.Common;
using CCE.Application.PlatformSettings.Dtos;
using MediatR;

namespace CCE.Application.PlatformSettings.Queries.GetFaqById;

public sealed record GetFaqByIdQuery(System.Guid Id) : IRequest<Response<FaqDto?>>;
