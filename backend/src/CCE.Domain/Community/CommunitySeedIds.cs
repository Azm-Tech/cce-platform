namespace CCE.Domain.Community;

/// <summary>
/// Well-known fixed identifiers for seeded/backfilled community data. The "General" community is
/// the default container that pre-existing posts are backfilled into (migration default + seeder).
/// </summary>
public static class CommunitySeedIds
{
    public static readonly System.Guid GeneralCommunityId = new("c0ffee00-0000-0000-0000-000000000001");
}
