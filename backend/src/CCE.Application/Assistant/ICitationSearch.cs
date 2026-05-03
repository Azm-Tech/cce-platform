namespace CCE.Application.Assistant;

/// <summary>
/// RAG-lite source for assistant citations. Looks up the most-relevant
/// Resource and KnowledgeMapNode rows for a given user/assistant
/// exchange, returning up to one citation of each kind.
/// </summary>
public interface ICitationSearch
{
    Task<IReadOnlyList<CitationDto>> FindCitationsAsync(
        string userQuestion,
        string assistantReply,
        string locale,
        CancellationToken ct);
}
