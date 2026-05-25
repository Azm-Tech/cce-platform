using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Application.PlatformSettings.Dtos;
using CCE.Domain.PlatformSettings;
using MediatR;

namespace CCE.Application.PlatformSettings.Queries.GetPoliciesSettings;

public sealed class GetPoliciesSettingsQueryHandler
    : IRequestHandler<GetPoliciesSettingsQuery, Response<PoliciesSettingsDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetPoliciesSettingsQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<PoliciesSettingsDto>> Handle(
        GetPoliciesSettingsQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.PoliciesSettings.ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var settings = list.FirstOrDefault();
        if (settings is null)
            return _msg.PoliciesSettingsNotFound<PoliciesSettingsDto>();

        var sections = await _db.PolicySections
            .Where(s => s.PoliciesSettingsId == settings.Id)
            .OrderBy(s => s.OrderIndex)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        return _msg.Ok(new PoliciesSettingsDto(
            settings.Id,
            sections.Select(s => new PolicySectionDto(
                s.Id, (int)s.Type,
                new LocalizedTextDto(s.Title.Ar, s.Title.En),
                new LocalizedTextDto(s.Content.Ar, s.Content.En),
                s.OrderIndex)).ToList()), "ITEMS_LISTED");
    }
}
