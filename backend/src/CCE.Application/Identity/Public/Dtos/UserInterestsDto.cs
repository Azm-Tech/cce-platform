using CCE.Application.InterestManagement.Dtos;

namespace CCE.Application.Identity.Public.Dtos;

public sealed record UserInterestsDto(
    IReadOnlyList<InterestTopicDto> CarbonAreaTopics,
    InterestTopicDto? KnowledgeAssessmentTopic,
    InterestTopicDto? JobSectorTopic,
    System.Guid? TargetCountryId);