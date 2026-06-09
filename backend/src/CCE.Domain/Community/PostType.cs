namespace CCE.Domain.Community;

/// <summary>US026 post kind. Fixed at creation and never changed (D4). A <see cref="Poll"/> post owns
/// exactly one poll; Info/Question own none.</summary>
public enum PostType
{
    Info = 0,
    Question = 1,
    Poll = 2,
}
