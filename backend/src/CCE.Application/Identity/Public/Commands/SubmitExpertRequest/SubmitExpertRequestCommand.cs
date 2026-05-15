using CCE.Application.Common;
using CCE.Application.Identity.Public.Dtos;
using MediatR;

namespace CCE.Application.Identity.Public.Commands.SubmitExpertRequest;

public sealed record SubmitExpertRequestCommand(
    System.Guid RequesterId,
    string RequestedBioAr,
    string RequestedBioEn,
    IReadOnlyList<string> RequestedTags) : IRequest<Result<ExpertRequestStatusDto>>;
