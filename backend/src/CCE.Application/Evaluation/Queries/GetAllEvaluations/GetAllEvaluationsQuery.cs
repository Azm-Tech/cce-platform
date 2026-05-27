using CCE.Application.Common;
using CCE.Application.Common.Pagination;
using CCE.Application.Evaluation.DTOs;
using MediatR;

namespace CCE.Application.Evaluation.Queries.GetAllEvaluations;

public sealed record GetAllEvaluationsQuery(
    int Page = 1,                             
    int PageSize = 20)                         
    : IRequest<Response<PagedResult<ServiceEvaluationDto>>>; 
