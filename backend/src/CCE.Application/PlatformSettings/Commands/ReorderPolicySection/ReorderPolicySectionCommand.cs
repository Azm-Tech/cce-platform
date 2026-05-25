using CCE.Application.Common;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.ReorderPolicySection;

public sealed record ReorderPolicySectionCommand(
    System.Guid Id,
    int OrderIndex) : IRequest<Response<System.Guid>>;
