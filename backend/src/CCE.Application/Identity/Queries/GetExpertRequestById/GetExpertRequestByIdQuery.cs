using CCE.Application.Common;
using CCE.Application.Identity.Dtos;
using MediatR;

namespace CCE.Application.Identity.Queries.GetExpertRequestById;

public sealed record GetExpertRequestByIdQuery(System.Guid Id)
    : IRequest<Response<ExpertRequestDto>>;
