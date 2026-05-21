using CCE.Application.Common;
using CCE.Application.PlatformSettings.Dtos;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.CreatePolicySection;

public sealed record CreatePolicySectionCommand(
    int Type,
    string TitleAr,
    string TitleEn,
    string ContentAr,
    string ContentEn) : IRequest<Response<PolicySectionDto>>;
