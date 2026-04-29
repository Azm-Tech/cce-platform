using CCE.Application.Common.Interfaces;
using CCE.Application.Reports;
using CCE.Application.Reports.Rows;
using CCE.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Reports;

public sealed class UserRegistrationsReportService : IUserRegistrationsReportService
{
    private readonly ICceDbContext _db;

    public UserRegistrationsReportService(ICceDbContext db)
    {
        _db = db;
    }

    public async System.Collections.Generic.IAsyncEnumerable<UserRegistrationRow> QueryAsync(
        System.DateTimeOffset? from,
        System.DateTimeOffset? to,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        // Users don't have a CreatedOn column; return all users for now (date filter no-op).
        // If a CreatedOn is added later, apply: query.Where(u => u.CreatedOn >= from && u.CreatedOn <= to).
        _ = from;
        _ = to;

        // Eager-load the role names for the page-of-users via a JOIN. For streaming, we materialize
        // userIds into a hash and fan out: but a streaming join requires a single SQL query.
        // Pragma: build the IAsyncEnumerable<UserRegistrationRow> from a LINQ projection that EF translates.
        var query = from u in _db.Users
                    select new
                    {
                        u.Id, u.Email, u.UserName, u.LockoutEnabled, u.LockoutEnd,
                        u.LocalePreference, u.CountryId,
                        Roles = (from ur in _db.UserRoles
                                 join r in _db.Roles on ur.RoleId equals r.Id
                                 where ur.UserId == u.Id
                                 select r.Name).ToList()
                    };

        var now = System.DateTimeOffset.UtcNow;
        await foreach (var row in StreamAsAsyncEnumerable(query).WithCancellation(ct).ConfigureAwait(false))
        {
            yield return new UserRegistrationRow
            {
                Id = row.Id,
                Email = row.Email,
                UserName = row.UserName,
                Roles = string.Join("; ", row.Roles.Where(r => r != null)),
                IsActive = !row.LockoutEnabled || row.LockoutEnd is null || row.LockoutEnd < now,
                LocalePreference = row.LocalePreference,
                CountryId = row.CountryId?.ToString(),
            };
        }
    }

    private static async System.Collections.Generic.IAsyncEnumerable<T> StreamAsAsyncEnumerable<T>(IQueryable<T> query)
    {
        if (query is System.Collections.Generic.IAsyncEnumerable<T> async)
        {
            await foreach (var item in async)
            {
                yield return item;
            }
        }
        else
        {
            foreach (var item in query)
            {
                yield return item;
            }
        }
    }
}
