using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Application.PlatformSettings.Dtos;
using CCE.Domain.PlatformSettings;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.UpdatePoliciesSettings;

public sealed class UpdatePoliciesSettingsCommandHandler
    : IRequestHandler<UpdatePoliciesSettingsCommand, Response<PoliciesSettingsDto>>
{
    private readonly IPoliciesSettingsRepository _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public UpdatePoliciesSettingsCommandHandler(
        IPoliciesSettingsRepository repo, ICceDbContext db, MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _msg = msg;
    }

    public async Task<Response<PoliciesSettingsDto>> Handle(
        UpdatePoliciesSettingsCommand request, CancellationToken cancellationToken)
    {
        var settings = await _repo.GetAsync(cancellationToken).ConfigureAwait(false);
        if (settings is null)
            return _msg.PoliciesSettingsNotFound<PoliciesSettingsDto>();

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var sections = await _db.PolicySections
            .Where(s => s.PoliciesSettingsId == settings.Id)
            .OrderBy(s => s.OrderIndex)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        return _msg.Ok(new PoliciesSettingsDto(
            settings.Id,
            sections.Select(s => new PolicySectionDto(
                s.Id, (int)s.Type, s.TitleAr, s.TitleEn,
                s.ContentAr, s.ContentEn, s.OrderIndex)).ToList(),
            Convert.ToBase64String(settings.RowVersion)), "SETTINGS_UPDATED");
    }
}
