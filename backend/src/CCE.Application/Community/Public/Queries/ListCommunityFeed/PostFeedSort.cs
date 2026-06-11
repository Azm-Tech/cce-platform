namespace CCE.Application.Community.Public.Queries.ListCommunityFeed;

/// <summary>Ordering options for the community home feed.</summary>
public enum PostFeedSort
{
    /// <summary>Reddit-style hot rank (<c>Post.Score</c> desc) — the default.</summary>
    Hot = 0,

    /// <summary>Most recently published first.</summary>
    Newest = 1,

    /// <summary>Highest up-vote count first.</summary>
    TopVoted = 2,
}
