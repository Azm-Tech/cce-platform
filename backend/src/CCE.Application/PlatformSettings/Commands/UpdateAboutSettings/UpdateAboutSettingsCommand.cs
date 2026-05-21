using CCE.Application.Common;
using CCE.Application.PlatformSettings.Dtos;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.UpdateAboutSettings;

public sealed record UpdateAboutSettingsCommand(
    string DescriptionAr,
    string DescriptionEn,
    string? HowToUseVideoUrl,
    byte[] RowVersion) : IRequest<Response<AboutSettingsDto>>;
