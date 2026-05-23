using CCE.Application.Common;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.UpdateHomepageSettings;

public sealed record UpdateHomepageSettingsCommand(
    string? VideoUrl,
    string ObjectiveAr,
    string ObjectiveEn,
    string CceConceptsAr,
    string CceConceptsEn,
    System.Collections.Generic.IReadOnlyList<System.Guid> ParticipatingCountryIds) : IRequest<Response<System.Guid>>;
