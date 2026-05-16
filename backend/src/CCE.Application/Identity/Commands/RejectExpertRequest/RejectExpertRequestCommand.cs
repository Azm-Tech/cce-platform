using CCE.Application.Common;
using CCE.Application.Identity.Dtos;
using MediatR;

namespace CCE.Application.Identity.Commands.RejectExpertRequest;

public sealed record RejectExpertRequestCommand(
    System.Guid Id,
    string RejectionReasonAr,
    string RejectionReasonEn) : IRequest<Response<ExpertRequestDto>>;
