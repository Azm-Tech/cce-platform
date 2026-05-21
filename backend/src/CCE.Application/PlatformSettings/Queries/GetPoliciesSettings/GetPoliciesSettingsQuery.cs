using CCE.Application.Common;
using CCE.Application.PlatformSettings.Dtos;
using MediatR;

namespace CCE.Application.PlatformSettings.Queries.GetPoliciesSettings;

public sealed record GetPoliciesSettingsQuery() : IRequest<Response<PoliciesSettingsDto>>;
