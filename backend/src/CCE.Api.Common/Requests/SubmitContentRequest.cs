using CCE.Application.Content.Commands.SubmitCountryContentRequest;

namespace CCE.Api.Common.Requests;

public sealed record SubmitContentRequest(
    System.Guid? CountryId,
    ContentBody Content);
