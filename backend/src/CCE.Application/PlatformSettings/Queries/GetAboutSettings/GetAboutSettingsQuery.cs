using CCE.Application.Common;
using CCE.Application.PlatformSettings.Dtos;
using MediatR;

namespace CCE.Application.PlatformSettings.Queries.GetAboutSettings;

public sealed record GetAboutSettingsQuery() : IRequest<Response<AboutSettingsDto>>;
