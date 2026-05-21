using CCE.Application.Common;
using CCE.Application.PlatformSettings.Dtos;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.UpdateHomepageSettings;

public sealed record UpdateHomepageSettingsCommand(
    string? VideoUrl,
    string ObjectiveAr,
    string ObjectiveEn,
    string CceConceptsAr,
    string CceConceptsEn,
    System.Collections.Generic.IReadOnlyList<System.Guid> ParticipatingCountryIds,
    byte[] RowVersion) : IRequest<Response<HomepageSettingsDto>>;
