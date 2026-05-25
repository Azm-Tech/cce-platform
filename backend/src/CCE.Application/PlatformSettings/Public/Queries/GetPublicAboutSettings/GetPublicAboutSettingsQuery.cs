using CCE.Application.Common;
using CCE.Application.PlatformSettings.Public.Dtos;
using MediatR;

namespace CCE.Application.PlatformSettings.Public.Queries.GetPublicAboutSettings;

public sealed record GetPublicAboutSettingsQuery() : IRequest<Response<PublicAboutSettingsDto>>;
