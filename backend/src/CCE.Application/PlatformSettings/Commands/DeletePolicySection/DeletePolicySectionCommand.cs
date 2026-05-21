using CCE.Application.Common;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.DeletePolicySection;

public sealed record DeletePolicySectionCommand(System.Guid Id) : IRequest<Response<VoidData>>;
