using System.Diagnostics.CodeAnalysis;
using CCE.Application.Search;
using CCE.Domain.Common;
using CCE.Domain.Surveys;
using CCE.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace CCE.Infrastructure.Search;

public sealed class SearchQueryLogger : ISearchQueryLogger
{
    private readonly CceDbContext _db;
    private readonly ISystemClock _clock;
    private readonly ILogger<SearchQueryLogger> _logger;

    public SearchQueryLogger(CceDbContext db, ISystemClock clock, ILogger<SearchQueryLogger> logger)
    {
        _db = db;
        _clock = clock;
        _logger = logger;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Analytics writes are best-effort; any failure is logged as a warning and must not propagate to callers.")]
    public async Task RecordAsync(
        System.Guid? userId,
        string queryText,
        int resultsCount,
        int responseTimeMs,
        string locale,
        CancellationToken ct)
    {
        try
        {
            var row = SearchQueryLog.Record(userId, queryText, resultsCount, responseTimeMs, locale, _clock);
            _db.Set<SearchQueryLog>().Add(row);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (System.Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record search query log");
        }
    }
}
