using CCE.Application.Identity;
using CCE.Domain.Identity;
using CCE.Domain.Verification;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly CceDbContext _db;

    public UserRepository(CceDbContext db) => _db = db;

    public async Task<Guid?> FindUserIdByContactAsync(string contact, OtpVerificationType type, CancellationToken ct)
    {
        return type switch
        {
            OtpVerificationType.Email => await _db.Users
                .Where(u => u.Email == contact)
                .Select(u => (Guid?)u.Id)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false),
            OtpVerificationType.Sms => await _db.Users
                .Where(u => u.PhoneNumber == contact)
                .Select(u => (Guid?)u.Id)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false),
            _ => null,
        };
    }

    public async Task StampConfirmedAsync(Guid userId, OtpVerificationType type, CancellationToken ct)
    {
        var stamp = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.ConcurrencyStamp)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var stub = new User { Id = userId, ConcurrencyStamp = stamp ?? string.Empty };
        _db.Attach(stub);
        if (type == OtpVerificationType.Email)
            stub.EmailConfirmed = true;
        else
            stub.PhoneNumberConfirmed = true;
    }
}
