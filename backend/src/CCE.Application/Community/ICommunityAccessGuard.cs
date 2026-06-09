namespace CCE.Application.Community;

/// <summary>
/// Centralizes the public/private community access rule (D6). Read: public communities are open;
/// private require membership. Post: members only. Moderate: community moderators.
/// (Admin-facing actions are gated by permissions at the endpoint, not by this guard.)
/// </summary>
public interface ICommunityAccessGuard
{
    Task<bool> CanReadAsync(Guid communityId, Guid? userId, CancellationToken ct);
    Task<bool> CanPostAsync(Guid communityId, Guid userId, CancellationToken ct);
    Task<bool> CanModerateAsync(Guid communityId, Guid userId, CancellationToken ct);
}
