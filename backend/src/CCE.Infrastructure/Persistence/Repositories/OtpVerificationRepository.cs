using CCE.Application.Verification;
using CCE.Domain.Verification;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Persistence.Repositories;

public sealed class OtpVerificationRepository
    : Repository<OtpVerification, Guid>, IOtpVerificationRepository
{
    public OtpVerificationRepository(CceDbContext db) : base(db) { }

    public async Task<OtpVerification?> FindActiveAsync(
        string contact, OtpVerificationType typeId, DateTimeOffset now, CancellationToken ct)
        => await Db.OtpVerifications
            .Where(o => o.Contact == contact
                     && o.TypeId == typeId
                     && !o.IsVerified
                     && !o.IsInvalidated
                     && o.ExpiresAt > now)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
}
