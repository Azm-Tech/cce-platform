<#
.SYNOPSIS
  Large-scale performance test with 10,000+ posts in the dataset.

  PREREQUISITE:
    dotnet run --project src/CCE.Seeder -- --bulk

  Phases:
    1  Pre-flight     -- API health + post count verification
    2  SQL cold path  -- global feed (no communityId -> SQL always), various sorts
    3  Redis warm     -- community feed warm-up then hot/newest Redis reads
    4  Personal feed  -- follow a bulk author, measure SQL fan-in latency
    5  Vote storm     -- find expert post, multi-user vote + comment, notification timing
    6  Summary        -- per-phase p50/p95 report
#>

$ErrorActionPreference = "Continue"
$BaseUrl              = "http://localhost:5001"
$GeneralCommunityId   = "c0ffee00-0000-0000-0000-000000000001"
$TokAdmin             = "Bearer dev:cce-admin"
$TokExpert            = "Bearer dev:cce-expert"
$TokUser              = "Bearer dev:cce-user"

# --- telemetry -----------------------------------------------------------
$PhaseTimings  = @{}
$CurrentPhase  = "init"
$AllTimings    = [System.Collections.Generic.List[int]]::new()
$TotalCalls    = 0
$TotalOK       = 0
$Gaps          = [System.Collections.Generic.List[hashtable]]::new()

function Start-Phase([string]$Name) {
    $script:CurrentPhase = $Name
    $script:PhaseTimings[$Name] = [System.Collections.Generic.List[int]]::new()
    Write-Host "`n=== $Name ===" -ForegroundColor Cyan
}

function Invoke-Api {
    param(
        [string]$Method    = "GET",
        [string]$Url,
        [string]$Token     = $TokAdmin,
        [object]$Body,
        [string]$Label,
        [switch]$AllowFail
    )
    $script:TotalCalls++
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $h = @{ Authorization = $Token; "Content-Type" = "application/json" }
        $p = @{ Method = $Method; Uri = "$BaseUrl$Url"; Headers = $h; TimeoutSec = 180; UseBasicParsing = $true }
        if ($Body) { $p.Body = ($Body | ConvertTo-Json -Depth 10) }
        $r = Invoke-WebRequest @p
        $sw.Stop(); $ms = [int]$sw.ElapsedMilliseconds
        $script:AllTimings.Add($ms)
        $script:PhaseTimings[$script:CurrentPhase].Add($ms)
        $script:TotalOK++
        $lbl = if ($Label) { $Label } else { "$Method $Url" }
        Write-Host ("  [{0,6}ms] {1}" -f $ms, $lbl)
        return ($r.Content | ConvertFrom-Json)
    } catch {
        $sw.Stop(); $ms = [int]$sw.ElapsedMilliseconds
        $script:AllTimings.Add($ms)
        $script:PhaseTimings[$script:CurrentPhase].Add($ms)
        $status = 0
        try { $status = [int]$_.Exception.Response.StatusCode } catch {}
        $lbl = if ($Label) { $Label } else { "$Method $Url" }
        $color = if ($AllowFail) { "DarkYellow" } else { "Red" }
        Write-Host ("  [{0,6}ms] {1}  HTTP {2}" -f $ms, $lbl, $status) -ForegroundColor $color
        if (-not $AllowFail) {
            $script:Gaps.Add(@{ Phase = $script:CurrentPhase; Label = $lbl; Status = $status; Ms = $ms })
        }
        return $null
    }
}

function Get-Pct([System.Collections.Generic.List[int]]$List, [int]$Pct) {
    if ($List.Count -eq 0) { return 0 }
    $s = $List | Sort-Object
    $idx = [math]::Max(0, [int][math]::Ceiling($s.Count * $Pct / 100.0) - 1)
    return $s[$idx]
}

function Show-PhaseStats([string]$Name) {
    $t = $script:PhaseTimings[$Name]
    if (-not $t -or $t.Count -eq 0) { Write-Host "  (no data)" ; return }
    $avg = [int](($t | Measure-Object -Average).Average)
    $p50 = Get-Pct $t 50
    $p95 = Get-Pct $t 95
    $max = ($t | Measure-Object -Maximum).Maximum
    Write-Host ("    avg={0}ms  p50={1}ms  p95={2}ms  max={3}ms  n={4}" -f $avg, $p50, $p95, $max, $t.Count)
}

function Assert-Ok([object]$Val, [string]$Label) {
    if ($Val) { Write-Host ("  [PASS] {0}" -f $Label) -ForegroundColor Green }
    else      { Write-Host ("  [FAIL] {0}" -f $Label) -ForegroundColor Red }
}

# =========================================================================
# Phase 1 -- Pre-flight
# =========================================================================
Start-Phase "Phase 1: Pre-flight"

$health = Invoke-Api -Url "/api/community/feed?page=1&pageSize=1&sort=1" -Label "API health (global feed)"
if (-not $health) {
    Write-Host "  API not responding. Start with:" -ForegroundColor Red
    Write-Host "    dotnet run --project src/CCE.Api.External --urls http://localhost:5001" -ForegroundColor Yellow
    exit 1
}

$totalPosts = 0
try { $totalPosts = [int]$health.data.total } catch {}
$color = if ($totalPosts -ge 10000) { "Green" } else { "Yellow" }
Write-Host ("  Total published posts: {0}" -f $totalPosts) -ForegroundColor $color

if ($totalPosts -lt 1000) {
    Write-Host "  [WARN] Dataset small. For full perf test run:" -ForegroundColor Yellow
    Write-Host "    dotnet run --project src/CCE.Seeder -- --bulk" -ForegroundColor Yellow
}

# =========================================================================
# Phase 2 -- SQL cold path (global feed, no communityId -> always SQL)
# =========================================================================
Start-Phase "Phase 2: SQL cold path"

Write-Host "  Global feed Newest (SQL, no Redis):  5 requests x pageSize=20"
1..5 | ForEach-Object {
    Invoke-Api -Url "/api/community/feed?page=1&pageSize=20&sort=1" -Label "Global Newest p1" | Out-Null
}

Write-Host "  Global feed TopVoted (SQL, no Redis): 5 requests"
1..5 | ForEach-Object {
    Invoke-Api -Url "/api/community/feed?page=1&pageSize=20&sort=2" -Label "Global TopVoted p1" | Out-Null
}

Write-Host "  Deep pagination (page=5, SQL OFFSET): 3 requests"
1..3 | ForEach-Object {
    Invoke-Api -Url "/api/community/feed?page=5&pageSize=20&sort=1" -Label "Global Newest p5" | Out-Null
}

Show-PhaseStats "Phase 2: SQL cold path"

# =========================================================================
# Phase 3 -- Community feed: cold SQL fallback -> Redis warm
# =========================================================================
Start-Phase "Phase 3: Community feed (cold then warm)"

Write-Host "  Requests 1-2 may be slow (Redis cold, SQL fallback + Redis write)..."
1..2 | ForEach-Object {
    Invoke-Api -Url "/api/community/feed?communityId=$GeneralCommunityId&page=1&pageSize=20&sort=1" -Label "Community Newest p1 (warming)" | Out-Null
}
Write-Host "  Requests 3-7 should be faster (Redis warm)..."
1..5 | ForEach-Object {
    Invoke-Api -Url "/api/community/feed?communityId=$GeneralCommunityId&page=1&pageSize=20&sort=1" -Label "Community Newest p1 (warm)" | Out-Null
}

Write-Host "  Hot leaderboard (Redis trim=1000): 3 requests"
1..3 | ForEach-Object {
    Invoke-Api -Url "/api/community/feed?communityId=$GeneralCommunityId&page=1&pageSize=20&sort=0" -Label "Community Hot p1" | Out-Null
}

Show-PhaseStats "Phase 3: Community feed (cold then warm)"

# =========================================================================
# Phase 4 -- Personal feed (SQL fan-in: WHERE authorId IN followedUserIds)
# =========================================================================
Start-Phase "Phase 4: Personal feed (fan-in)"

# cce-user follows cce-admin (regular author with many bulk posts)
$adminId = "aaaaaaaa-aaaa-aaaa-aaaa-000000000001"
Invoke-Api -Method "POST" -Url "/api/me/following/$adminId" -Token $TokUser -Label "User follows admin" -AllowFail | Out-Null
Invoke-Api -Method "POST" -Url "/api/me/community/$GeneralCommunityId/join" -Token $TokUser -Label "User joins General" -AllowFail | Out-Null

Write-Host "  Personal feed (SQL WHERE authorId IN followed): 7 requests"
1..7 | ForEach-Object {
    Invoke-Api -Url "/api/me/feed?page=1&pageSize=20" -Token $TokUser -Label "Personal feed p1" | Out-Null
}

Write-Host "  Personal feed deep pages: p3, p5, p10"
foreach ($pg in 3, 5, 10) {
    Invoke-Api -Url "/api/me/feed?page=$pg&pageSize=20" -Token $TokUser -Label "Personal feed p$pg" | Out-Null
}

Show-PhaseStats "Phase 4: Personal feed (fan-in)"

# =========================================================================
# Phase 5 -- Vote storm: expert post + multi-user votes/comments + notifications
# =========================================================================
Start-Phase "Phase 5: Vote storm + notifications"

# Find an expert post in the feed (isExpert = true).
$expertPostId = $null
$expertTopicId = $null
for ($pg = 1; $pg -le 5 -and -not $expertPostId; $pg++) {
    $r = Invoke-Api -Url "/api/community/feed?page=$pg&pageSize=20&sort=1" -Label "Scan feed page $pg for expert post"
    if ($r -and $r.data -and $r.data.items) {
        $hit = $r.data.items | Where-Object { $_.isExpert -eq $true } | Select-Object -First 1
        if ($hit) {
            $expertPostId = $hit.id
            $expertTopicId = $hit.topicId
        }
    }
}

if (-not $expertPostId) {
    # No expert post from bulk seeder -- create one with cce-expert.
    Write-Host "  No expert post found in feed -- creating one..." -ForegroundColor Yellow
    $feedItem = Invoke-Api -Url "/api/community/feed?page=1&pageSize=1&sort=1" -Label "Get topicId for new post"
    if ($feedItem -and $feedItem.data -and $feedItem.data.items.Count -gt 0) {
        $expertTopicId = $feedItem.data.items[0].topicId
    }
    if ($expertTopicId) {
        $created = Invoke-Api -Method "POST" -Url "/api/community/posts" -Token $TokExpert -Label "Expert creates post" -Body @{
            communityId = $GeneralCommunityId
            topicId     = $expertTopicId
            type        = 1
            title       = "Expert post for large-scale vote-storm test"
            content     = "Measuring notification delivery timing with 10k posts in the dataset."
            locale      = "en"
        } -AllowFail
        if ($created -and $created.data -and $created.data.id) {
            $expertPostId = $created.data.id
        }
    }
}

if ($expertPostId) {
    Write-Host ("  Expert post ID: {0}" -f $expertPostId)

    # Baseline unread count for the expert.
    $before = Invoke-Api -Url "/api/me/notifications/unread-count" -Token $TokExpert -Label "Expert unread count (before)"
    $unreadBefore = 0
    try { $unreadBefore = [int]$before.data.count } catch {}
    Write-Host ("    Unread before: {0}" -f $unreadBefore)

    # Admin and user vote + comment.
    Invoke-Api -Method "POST" -Url "/api/community/posts/$expertPostId/vote" -Token $TokAdmin -Body @{ direction = 1 } -Label "Admin upvotes expert post" -AllowFail | Out-Null
    Invoke-Api -Method "POST" -Url "/api/community/posts/$expertPostId/vote" -Token $TokUser  -Body @{ direction = 1 } -Label "User upvotes expert post"  -AllowFail | Out-Null

    Invoke-Api -Method "POST" -Url "/api/community/posts/$expertPostId/replies" -Token $TokAdmin -Label "Admin comments" -Body @{
        content = "Admin comment for notification storm test - large dataset."
        locale  = "en"
    } -AllowFail | Out-Null

    Invoke-Api -Method "POST" -Url "/api/community/posts/$expertPostId/replies" -Token $TokUser -Label "User comments" -Body @{
        content = "User comment for notification storm test - large dataset."
        locale  = "en"
    } -AllowFail | Out-Null

    # Check notification delivery (3 reads to measure query latency under large dataset).
    1..3 | ForEach-Object {
        $after = Invoke-Api -Url "/api/me/notifications/unread-count" -Token $TokExpert -Label "Expert unread count (after)"
        $unreadAfter = 0
        try { $unreadAfter = [int]$after.data.count } catch {}
        Write-Host ("    Unread after vote+comment: {0}" -f $unreadAfter)
    }

    # Measure notification list query with large dataset.
    Write-Host "  Notification list latency (3 requests):"
    1..3 | ForEach-Object {
        Invoke-Api -Url "/api/me/notifications?page=1&pageSize=10" -Token $TokExpert -Label "Expert notifications list" | Out-Null
    }

    # Verify post score/vote count updated (Redis meta + SQL).
    Invoke-Api -Url "/api/community/posts/$expertPostId" -Label "Post detail after votes" | Out-Null
} else {
    Write-Host "  [WARN] Could not obtain expert post -- skipping vote storm." -ForegroundColor Yellow
}

Show-PhaseStats "Phase 5: Vote storm + notifications"

# =========================================================================
# Phase 6 -- Summary
# =========================================================================
$p50all = Get-Pct $AllTimings 50
$p95all = Get-Pct $AllTimings 95
$avgAll = if ($AllTimings.Count -gt 0) { [int](($AllTimings | Measure-Object -Average).Average) } else { 0 }
$maxAll = if ($AllTimings.Count -gt 0) { ($AllTimings | Measure-Object -Maximum).Maximum } else { 0 }

Write-Host "`n$("="*72)" -ForegroundColor White
Write-Host ("Large-Scale Perf  |  dataset={0} posts  calls={1}  ok={2}  gaps={3}" -f $totalPosts, $TotalCalls, $TotalOK, $Gaps.Count) -ForegroundColor White
Write-Host ("Overall  avg={0}ms  p50={1}ms  p95={2}ms  max={3}ms" -f $avgAll, $p50all, $p95all, $maxAll) -ForegroundColor White
Write-Host ""

foreach ($ph in @("Phase 2: SQL cold path", "Phase 3: Community feed (cold then warm)", "Phase 4: Personal feed (fan-in)", "Phase 5: Vote storm + notifications")) {
    Write-Host ("  {0}" -f $ph)
    Show-PhaseStats $ph
}

if ($Gaps.Count -gt 0) {
    Write-Host "`nGAPS ($($Gaps.Count)):" -ForegroundColor Red
    foreach ($g in $Gaps) {
        Write-Host ("  [{0}] {1}  HTTP {2}  {3}ms" -f $g.Phase, $g.Label, $g.Status, $g.Ms) -ForegroundColor Red
    }
} else {
    Write-Host "`nNo gaps -- all measured calls succeeded." -ForegroundColor Green
}

# Flag slow phases.
$slowThreshold = 20000
foreach ($ph in $PhaseTimings.Keys) {
    $t = $PhaseTimings[$ph]
    if ($t -and $t.Count -gt 0) {
        $p95 = Get-Pct $t 95
        if ($p95 -gt $slowThreshold) {
            Write-Host ("  [SLOW] {0}: p95={1}ms (>{2}ms) -- worth investigating" -f $ph, $p95, $slowThreshold) -ForegroundColor Yellow
        }
    }
}
