using CCE.Domain.Identity;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models.ODataErrors;

namespace CCE.Infrastructure.Identity;

/// <summary>
/// Sub-11d Task D — batch UPN→EntraIdObjectId backfill for CCE Users
/// that don't have an objectId linked yet. Same logic as
/// <c>EntraIdUserResolver</c> (in CCE.Api.Common.Auth) but operates
/// over the whole Users table in one pass instead of one user at a time.
///
/// Used by the operator on cutover day per
/// <c>docs/runbooks/entra-id-cutover.md</c> step 7. The lazy resolver
/// links each user on first sign-in; this batch endpoint pre-populates
/// the link before users see it, so first-sign-in is a single round
/// trip with no Graph lookup.
/// </summary>
public sealed class EntraIdUserSyncService
{
    private readonly EntraIdGraphClientFactory _graphFactory;
    private readonly CceDbContext _db;
    private readonly ILogger<EntraIdUserSyncService> _logger;

    public EntraIdUserSyncService(
        EntraIdGraphClientFactory graphFactory,
        CceDbContext db,
        ILogger<EntraIdUserSyncService> logger)
    {
        _graphFactory = graphFactory;
        _db = db;
        _logger = logger;
    }

    public async Task<EntraIdUserSyncSummary> SyncAsync(CancellationToken ct = default)
    {
        var graph = _graphFactory.Create();

        // Snapshot the unlinked users — we mutate the rows below.
        var unlinked = await _db.Users
            .Where(u => u.EntraIdObjectId == null)
            .ToListAsync(ct).ConfigureAwait(false);

        var totalScanned = unlinked.Count;
        var linked = 0;
        var notFoundInGraph = 0;
        var errors = 0;
        _logger.LogInformation("EntraIdUserSyncService: scanning {Count} unlinked users", totalScanned);

        foreach (var user in unlinked)
        {
            ct.ThrowIfCancellationRequested();
            var upn = user.Email ?? user.UserName;
            if (string.IsNullOrWhiteSpace(upn))
            {
                continue;
            }

            try
            {
                var match = await graph.Users.GetAsync(rb =>
                {
                    rb.QueryParameters.Filter = $"userPrincipalName eq '{upn.Replace("'", "''", StringComparison.Ordinal)}'";
                    rb.QueryParameters.Select = new[] { "id", "userPrincipalName" };
                    rb.QueryParameters.Top = 1;
                }, ct).ConfigureAwait(false);

                var first = match?.Value?.FirstOrDefault();
                if (first?.Id is null)
                {
                    notFoundInGraph++;
                    continue;
                }

                if (Guid.TryParse(first.Id, out var objectId))
                {
                    user.LinkEntraIdObjectId(objectId);
                    linked++;
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (ODataError ex)
            {
                _logger.LogWarning(ex, "Graph filter failed for {Upn}; skipping", upn);
                errors++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error resolving {Upn}; skipping", upn);
                errors++;
            }
#pragma warning restore CA1031
        }

        if (linked > 0)
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        _logger.LogInformation(
            "EntraIdUserSyncService: complete. scanned={Scanned} linked={Linked} notFound={NotFound} errors={Errors}",
            totalScanned, linked, notFoundInGraph, errors);

        return new EntraIdUserSyncSummary(totalScanned, linked, notFoundInGraph, errors);
    }
}

/// <summary>
/// Result of <see cref="EntraIdUserSyncService.SyncAsync"/>.
/// </summary>
public sealed record EntraIdUserSyncSummary(
    int TotalScanned,
    int Linked,
    int NotFoundInGraph,
    int Errors);
