using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Application.Notifications;
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.Domain.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Content.Commands.ToggleNewsFollow;

internal sealed class ToggleNewsFollowCommandHandler(
    IRepository<NewsFollow, System.Guid> _repo,
    IUserNotificationSettingsRepository _settingsRepo,
    ICceDbContext _db,
    ICurrentUserAccessor _currentUser,
    ISystemClock _clock,
    MessageFactory _msg)
    : IRequestHandler<ToggleNewsFollowCommand, Response<VoidData>>
{
    public async Task<Response<VoidData>> Handle(ToggleNewsFollowCommand request, CancellationToken ct)
    {
        var userId = _currentUser.GetUserId();
        if (userId is null)
            return _msg.Unauthorized<VoidData>("NOT_AUTHENTICATED");

        var existing = await _db.NewsFollows
            .FirstOrDefaultAsync(f => f.UserId == userId.Value, ct)
            .ConfigureAwait(false);

        if (existing is not null)
            _db.Attach(existing);

        if (existing is null)
        {
            var follow = NewsFollow.Follow(userId.Value, userId.Value, _clock);
            await _repo.AddAsync(follow, ct).ConfigureAwait(false);

            var settings = await _settingsRepo.GetAsync(
                userId.Value, NotificationChannel.InApp, "NEWS_PUBLISHED", ct)
                .ConfigureAwait(false);

            if (settings is null)
            {
                var ns = UserNotificationSettings.Create(
                    userId.Value, NotificationChannel.InApp, true, "NEWS_PUBLISHED");
                await _settingsRepo.AddAsync(ns, ct).ConfigureAwait(false);
            }
            else if (!settings.IsEnabled)
            {
                settings.Update(true);
            }

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return _msg.Ok("NEWS_FOLLOWED");
        }

        if (existing.Status == FollowStatus.Followed)
        {
            existing.Unfollow(userId.Value, _clock);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return _msg.Ok("NEWS_UNFOLLOWED");
        }

        existing.ReFollow(userId.Value, _clock);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return _msg.Ok("NEWS_FOLLOWED");
    }
}
