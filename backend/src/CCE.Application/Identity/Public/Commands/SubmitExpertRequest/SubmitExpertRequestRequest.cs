namespace CCE.Application.Identity.Public.Commands.SubmitExpertRequest;

public sealed record SubmitExpertRequestRequest(
    string RequestedBioAr,
    string RequestedBioEn,
    IReadOnlyList<string>? RequestedTags);
