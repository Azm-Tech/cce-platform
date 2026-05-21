using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Application.PlatformSettings.Dtos;
using CCE.Domain.PlatformSettings;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.CreatePolicySection;

public sealed class CreatePolicySectionCommandHandler
    : IRequestHandler<CreatePolicySectionCommand, Response<PolicySectionDto>>
{
    private readonly IPoliciesSettingsRepository _policiesRepo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public CreatePolicySectionCommandHandler(
        IPoliciesSettingsRepository policiesRepo, ICceDbContext db, MessageFactory msg)
    {
        _policiesRepo = policiesRepo;
        _db = db;
        _msg = msg;
    }

    public async Task<Response<PolicySectionDto>> Handle(
        CreatePolicySectionCommand request, CancellationToken cancellationToken)
    {
        var settings = await _policiesRepo.GetAsync(cancellationToken).ConfigureAwait(false);
        if (settings is null)
            return _msg.PoliciesSettingsNotFound<PolicySectionDto>();

        var type = (PolicySectionType)request.Type;

        var maxOrder = await _db.PolicySections
            .Where(s => s.PoliciesSettingsId == settings.Id)
            .Select(s => (int?)s.OrderIndex)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var nextOrder = (maxOrder.FirstOrDefault() ?? -1) + 1;

        var section = PolicySection.Create(
            settings.Id, type, request.TitleAr, request.TitleEn,
            request.ContentAr, request.ContentEn, nextOrder);

        _db.Add(section);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.Ok(new PolicySectionDto(
            section.Id, (int)section.Type, section.TitleAr, section.TitleEn,
            section.ContentAr, section.ContentEn, section.OrderIndex), "CONTENT_CREATED");
    }
}
