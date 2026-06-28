using CCE.Application.Common;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.UpdateAboutSettings;

public sealed record UpdateAboutSettingsCommand(
    string DescriptionAr,
    string DescriptionEn,
    string? HowToUseVideoUrl) : IRequest<Response<System.Guid>>;
