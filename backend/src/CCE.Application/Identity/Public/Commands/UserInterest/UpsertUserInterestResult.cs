using CCE.Application.InterestManagement.Dtos;

namespace CCE.Application.Identity.Public.Commands.UserInterest;

public sealed record UpsertUserInterestResult(
    IReadOnlyList<InterestTopicDto> CarbonAreaTopics,
    InterestTopicDto? KnowledgeAssessmentTopic,
    InterestTopicDto? JobSectorTopic,
    System.Guid? TargetCountryId);
