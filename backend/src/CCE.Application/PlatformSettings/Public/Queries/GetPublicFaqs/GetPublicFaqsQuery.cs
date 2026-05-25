using CCE.Application.Common;
using CCE.Application.PlatformSettings.Public.Dtos;
using MediatR;

namespace CCE.Application.PlatformSettings.Public.Queries.GetPublicFaqs;

public sealed record GetPublicFaqsQuery : IRequest<Response<IReadOnlyList<PublicFaqDto>>>;
