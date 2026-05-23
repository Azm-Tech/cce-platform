using CCE.Application.Common;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.CreatePolicySection;

public sealed record CreatePolicySectionCommand(
    int Type,
    string TitleAr,
    string TitleEn,
    string ContentAr,
    string ContentEn) : IRequest<Response<System.Guid>>;
