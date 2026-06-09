using CCE.Application.Common;
using CCE.Application.InterestManagement.Dtos;
using MediatR;

namespace CCE.Application.InterestManagement.Queries.GetInterestQuestions;

public sealed record GetInterestQuestionsQuery
    : IRequest<Response<IReadOnlyList<InterestCategoryInfoDto>>>;