namespace CCE.Application.Search;

public enum SearchableType
{
    News = 0,
    Events = 1,
    Resources = 2,
    Pages = 3,
    KnowledgeMaps = 4,

    // Community search — served by SearchCommunityPostsQueryHandler via /feed?q=
    // These are excluded from the global /api/search cross-content loop (see MeilisearchClient.GlobalSearchTypes).
    CommunityPosts   = 5,
    CommunityReplies = 6,
}
