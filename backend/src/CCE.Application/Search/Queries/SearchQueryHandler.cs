using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using MediatR;

namespace CCE.Application.Search.Queries;

public sealed class SearchQueryHandler : IRequestHandler<SearchQuery, PagedResult<SearchHitDto>>
{
    private readonly ISearchClient _client;
    private readonly ISearchQueryLogger _logger;
    private readonly ICurrentUserAccessor _currentUser;

    public SearchQueryHandler(ISearchClient client, ISearchQueryLogger logger, ICurrentUserAccessor currentUser)
    {
        _client = client;
        _logger = logger;
        _currentUser = currentUser;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Defensive double-catch inside fire-and-forget lambda; analytics failure must never propagate to the caller.")]
    public async Task<PagedResult<SearchHitDto>> Handle(SearchQuery request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var result = await _client.SearchAsync(request.Q, request.Type, request.Page, request.PageSize, cancellationToken).ConfigureAwait(false);
        sw.Stop();

        // Fire-and-forget analytics write; don't slow the search response.
        var userId = _currentUser.GetUserId();
        var elapsed = (int)sw.ElapsedMilliseconds;
        var locale = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        if (locale != "ar" && locale != "en") locale = "en";  // domain requires "ar" or "en"

        _ = Task.Run(async () =>
        {
            try { await _logger.RecordAsync(userId, request.Q, result.Items.Count, elapsed, locale, CancellationToken.None).ConfigureAwait(false); }
            catch (Exception) { /* swallowed in logger; defensive double-catch */ }
        }, CancellationToken.None);

        return result;
    }
}
