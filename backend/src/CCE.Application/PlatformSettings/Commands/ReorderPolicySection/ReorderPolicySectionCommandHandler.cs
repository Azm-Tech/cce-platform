using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.PlatformSettings;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.ReorderPolicySection;

public sealed class ReorderPolicySectionCommandHandler
    : IRequestHandler<ReorderPolicySectionCommand, Response<System.Guid>>
{
    private readonly IPoliciesSettingsRepository _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public ReorderPolicySectionCommandHandler(
        IPoliciesSettingsRepository repo,
        ICceDbContext db,
        MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _msg = msg;
    }

    public async Task<Response<System.Guid>> Handle(
        ReorderPolicySectionCommand request, CancellationToken cancellationToken)
    {
        var settings = await _repo.GetAsync(cancellationToken).ConfigureAwait(false);
        if (settings is null)
            return _msg.NotFound<System.Guid>(MessageKeys.PlatformSettings.POLICIES_SETTINGS_NOT_FOUND);

        var section = settings.Sections.FirstOrDefault(s => s.Id == request.Id);
        if (section is null)
            return _msg.NotFound<System.Guid>(MessageKeys.PlatformSettings.POLICY_SECTION_NOT_FOUND);

        settings.ReorderSection(section, request.OrderIndex);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.Ok(section.Id, MessageKeys.PlatformSettings.SECTION_REORDERED);
    }
}
