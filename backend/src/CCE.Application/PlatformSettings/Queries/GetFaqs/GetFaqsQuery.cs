using CCE.Application.Common;
using CCE.Application.PlatformSettings.Dtos;
using MediatR;

namespace CCE.Application.PlatformSettings.Queries.GetFaqs;

public sealed record GetFaqsQuery : IRequest<Response<IReadOnlyList<FaqDto>>>;
