namespace CCE.Application.Search;

public interface ISearchQueryLogger
{
    /// <summary>
    /// Records a search analytics row. Best-effort: implementations should swallow exceptions
    /// so analytics failure never breaks the search response.
    /// </summary>
    Task RecordAsync(
        System.Guid? userId,
        string queryText,
        int resultsCount,
        int responseTimeMs,
        string locale,
        CancellationToken ct);
}
