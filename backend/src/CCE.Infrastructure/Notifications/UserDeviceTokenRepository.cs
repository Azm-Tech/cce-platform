using System.Collections.Generic;
using System.Linq;
using CCE.Application.Notifications;
using CCE.Domain.Notifications;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Notifications;

public sealed class UserDeviceTokenRepository
    : EntityRepository<UserDeviceToken, System.Guid>, IUserDeviceTokenRepository
{
    public UserDeviceTokenRepository(CceDbContext db) : base(db) { }

    public async Task<IReadOnlyList<UserDeviceToken>> GetActiveByUserIdAsync(
        System.Guid userId, CancellationToken cancellationToken)
        => await Db.Set<UserDeviceToken>()
            .Where(t => t.UserId == userId && t.IsActive)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<UserDeviceToken?> GetByUserAndDeviceAsync(
        System.Guid userId, string deviceId, CancellationToken cancellationToken)
        => await Db.Set<UserDeviceToken>()
            .FirstOrDefaultAsync(t => t.UserId == userId && t.DeviceId == deviceId, cancellationToken)
            .ConfigureAwait(false);

    public override async Task AddAsync(UserDeviceToken token, CancellationToken ct)
        => await Db.Set<UserDeviceToken>().AddAsync(token, ct).ConfigureAwait(false);

    public async Task DeactivateByTokensAsync(
        IReadOnlyList<string> fcmTokens, CancellationToken cancellationToken)
    {
        var tokens = await Db.Set<UserDeviceToken>()
            .Where(t => fcmTokens.Contains(t.Token) && t.IsActive)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var t in tokens)
            t.Deactivate();
    }
}
