using CCE.Application.Common;
using CCE.Application.PlatformSettings.Dtos;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.UpdatePoliciesSettings;

public sealed record UpdatePoliciesSettingsCommand(
    byte[] RowVersion) : IRequest<Response<PoliciesSettingsDto>>;
