using CCE.Application.Common;
using CCE.Application.PlatformSettings.Public.Dtos;
using MediatR;

namespace CCE.Application.PlatformSettings.Public.Queries.GetPublicPoliciesSettings;

public sealed record GetPublicPoliciesSettingsQuery() : IRequest<Response<PublicPoliciesSettingsDto>>;
