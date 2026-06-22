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
    : IRequestHandler<GetUserRegistrationReportQuery, Response<UserRegistrationReportDto>>
{
    public async Task<Response<UserRegistrationReportDto>> Handle(
        GetUserRegistrationReportQuery q, CancellationToken ct)
    {
        var users = await _db.Users
            .Where(u => !u.IsDeleted)
            .Select(u => new UserRegistrationReportUserDto(
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.JobTitle,
                u.OrganizationName,
                u.PhoneNumber))
            .ToListAsyncEither(ct)
            .ConfigureAwait(false);

        var report = new UserRegistrationReportDto(
            ReportId: "RP001",
            ReportTitle: "تقرير تسجيل المستخدمين",
            GeneratedAt: DateTimeOffset.UtcNow,
            TotalUsers: users.Count,
            Users: users);

        return _msg.Ok(report, "ITEMS_LISTED");
    }
}
