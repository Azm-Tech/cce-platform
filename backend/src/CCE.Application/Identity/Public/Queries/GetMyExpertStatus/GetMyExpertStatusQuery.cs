using CCE.Application.Common;
using CCE.Application.Identity.Public.Dtos;
using MediatR;

namespace CCE.Application.Identity.Public.Queries.GetMyExpertStatus;

public sealed record GetMyExpertStatusQuery(System.Guid UserId) : IRequest<Response<ExpertRequestStatusDto>>;
