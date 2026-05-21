using CCE.Application.Common;
using CCE.Application.PlatformSettings.Dtos;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.UpdatePolicySection;

public sealed record UpdatePolicySectionCommand(
    System.Guid Id,
    string TitleAr,
    string TitleEn,
    string ContentAr,
    string ContentEn) : IRequest<Response<PolicySectionDto>>;
