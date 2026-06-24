using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Application.PlatformSettings.Dtos;
using CCE.Application.PlatformSettings.Public.Dtos;
using CCE.Domain.PlatformSettings;
using MediatR;

namespace CCE.Application.PlatformSettings.Public.Queries.GetPublicPoliciesSettings;

public sealed class GetPublicPoliciesSettingsQueryHandler
    : IRequestHandler<GetPublicPoliciesSettingsQuery, Response<PublicPoliciesSettingsDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetPublicPoliciesSettingsQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<PublicPoliciesSettingsDto>> Handle(
        GetPublicPoliciesSettingsQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.PoliciesSettings.ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var settings = list.FirstOrDefault();
        if (settings is null)
            return _msg.PoliciesSettingsNotFound<PublicPoliciesSettingsDto>();

        var sections = await _db.PolicySections
            .Where(s => s.PoliciesSettingsId == settings.Id)
            .OrderBy(s => s.OrderIndex)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        return _msg.Ok(new PublicPoliciesSettingsDto(
            sections.Select(s => new PublicPolicySectionDto(
                (int)s.Type,
                new LocalizedTextDto(s.Title.Ar, s.Title.En),
                new LocalizedTextDto(s.Content.Ar, s.Content.En))).ToList()), MessageKeys.General.ITEMS_LISTED);
    }
}
