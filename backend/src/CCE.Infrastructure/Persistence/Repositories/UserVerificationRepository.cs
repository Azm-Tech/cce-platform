using CCE.Application.Verification;
using CCE.Domain.Verification;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Persistence.Repositories;

public sealed class UserVerificationRepository
    : Repository<UserVerification, Guid>, IUserVerificationRepository
{
    public UserVerificationRepository(CceDbContext db) : base(db) { }

    public async Task<UserVerification?> FindAsync(
        string contact, OtpVerificationType typeId, CancellationToken ct)
        => await Db.UserVerifications
            .Where(v => v.Contact == contact && v.TypeId == typeId)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
}
