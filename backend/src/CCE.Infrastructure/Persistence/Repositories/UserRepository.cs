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

    public void StampConfirmed(Guid userId, OtpVerificationType type)
    {
        var stub = new User { Id = userId };
        _db.Attach(stub);
        if (type == OtpVerificationType.Email)
            stub.EmailConfirmed = true;
        else
            stub.PhoneNumberConfirmed = true;
    }
}
