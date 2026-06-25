using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Application.Reports.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Reports.Queries.GetUserPreferenceReport;

internal sealed class GetUserPreferenceReportQueryHandler(
    ICceDbContext _db,
    MessageFactory _msg)
    : IRequestHandler<GetUserPreferenceReportQuery, Response<List<UserPreferenceReportDto>>>
{
    public async Task<Response<List<UserPreferenceReportDto>>> Handle(
        GetUserPreferenceReportQuery q, CancellationToken ct)
    {
        var items = await _db.Users
            .Where(u => !u.IsDeleted)
            .OrderBy(u => u.Id)
            .Select(u => new UserPreferenceReportDto(
                u.Id,
                u.UserInterestTopics.Select(uit => uit.InterestTopicId).ToList(),
                (int)u.KnowledgeLevel,
                u.JobTitle,
                u.CountryId))
            .ToListAsyncEither(ct)
            .ConfigureAwait(false);

        return _msg.Ok(items, MessageKeys.General.ITEMS_LISTED);
    }
}
