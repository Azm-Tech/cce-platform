namespace CCE.Domain.Content;

/// <summary>
/// Publication type of a <c>Resource</c>. Drives UI rendering (icon + viewer)
/// and categorization in the resource center.
/// </summary>
public enum ResourceType
{
    Paper = 0,
    Article = 1,
    Study = 2,
    Presentation = 3,
    ScientificPaper = 4,
    Report = 5,
    Book = 6,
    Research = 7,
    CceGuide = 8,
    Media = 9,
}
