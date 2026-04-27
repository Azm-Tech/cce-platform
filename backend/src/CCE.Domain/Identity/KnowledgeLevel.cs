namespace CCE.Domain.Identity;

/// <summary>
/// Self-declared user knowledge level — drives content-recommendation defaults and
/// feeds the Knowledge Maps starting node selection.
/// </summary>
public enum KnowledgeLevel
{
    /// <summary>Default for new accounts.</summary>
    Beginner = 0,
    Intermediate = 1,
    Advanced = 2,
}
