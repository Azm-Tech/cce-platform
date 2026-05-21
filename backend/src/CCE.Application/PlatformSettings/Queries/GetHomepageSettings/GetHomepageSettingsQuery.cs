using CCE.Application.Common;
using CCE.Application.PlatformSettings.Dtos;
using MediatR;

namespace CCE.Application.PlatformSettings.Queries.GetHomepageSettings;

public sealed record GetHomepageSettingsQuery() : IRequest<Response<HomepageSettingsDto>>;
