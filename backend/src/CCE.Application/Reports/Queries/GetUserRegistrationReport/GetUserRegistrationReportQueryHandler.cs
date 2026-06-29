using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Application.Reports.Dtos;
using MediatR;

namespace CCE.Application.Reports.Queries.GetUserRegistrationReport;

internal sealed class GetUserRegistrationReportQueryHandler(
    ICceDbContext _db,
    MessageFactory _msg)
    : IRequestHandler<GetUserRegistrationReportQuery, Response<PagedResult<UserRegistrationReportUserDto>>>
{
    public async Task<Response<PagedResult<UserRegistrationReportUserDto>>> Handle(
        GetUserRegistrationReportQuery q, CancellationToken ct)
    {
        var query = _db.Users
            .Where(u => !u.IsDeleted);

        if (q.From.HasValue)
            query = query.Where(u => u.CreatedOn >= q.From.Value);
        if (q.To.HasValue)
            query = query.Where(u => u.CreatedOn <= q.To.Value);

        query = query.OrderByDescending(u => u.CreatedOn);

        var paged = await query.ToPagedResultAsync(
            u => new UserRegistrationReportUserDto(
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.JobTitle,
                u.OrganizationName,
                u.PhoneNumber),
            q.Page,
            q.PageSize,
            ct)
            .ConfigureAwait(false);

        return _msg.Ok(paged, MessageKeys.General.ITEMS_LISTED);
    }
}
