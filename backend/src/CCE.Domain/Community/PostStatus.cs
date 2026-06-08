namespace CCE.Domain.Community;

/// <summary>Draft → Published lifecycle (D9). Drafts are author-private and excluded from feeds.</summary>
public enum PostStatus
{
    Draft = 0,
    Published = 1,
}
