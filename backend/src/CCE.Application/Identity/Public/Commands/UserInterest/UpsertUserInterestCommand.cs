using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Identity.Public.Commands.UserInterest;

public sealed record UpsertUserInterestCommand(
    System.Guid UserId,
    IReadOnlyList<System.Guid>? CarbonAreaIds,
    System.Guid? KnowledgeAssessmentId,
    System.Guid? JobSectorId,
    System.Guid? TargetCountryId) : IRequest<Response<UpsertUserInterestResult>>;
