#Requires -Version 5.1
<#
.SYNOPSIS
    Full community cycle integration test: create -> vote -> comment -> delete -> notifications.

.DESCRIPTION
    Exercises every major community API path and records response times, counter
    accuracy, and feed/notification delivery gaps.

    Prerequisites:
    1. External API running:   dotnet run --project src/CCE.Api.External --urls http://localhost:5001
    2. Internal API running:   dotnet run --project src/CCE.Api.Internal --urls http://localhost:5002
    3. Database seeded:        dotnet run --project src/CCE.Seeder -- --demo

    Auth: DevAuth bearer shortcut  "Authorization: Bearer dev:<role>"
      Admin  (cce-admin)   - aaaaaaaa-aaaa-aaaa-aaaa-000000000001
      Expert (cce-expert)  - aaaaaaaa-aaaa-aaaa-aaaa-000000000004  (User1, post author)
      User   (cce-user)    - aaaaaaaa-aaaa-aaaa-aaaa-000000000005  (User2, voter/commenter)

.EXAMPLE
    .\test-community-cycle.ps1 -CommunityId "C0FFEE00-0000-0000-0000-000000000001"
    .\test-community-cycle.ps1 -ExtBase http://localhost:5001 -ReportPath .\report.md
#>
param(
    [string]$ExtBase     = "http://localhost:5001",
    [string]$IntBase     = "http://localhost:5002",
    [string]$ReportPath  = ".\community-cycle-report.md",
    [string]$CommunityId = ""
)

$ErrorActionPreference = "Continue"
function IntOrZero { param($v) if ($null -ne $v) { [int]$v } else { 0 } }


# Auth headers
$AdminAuth = "Bearer dev:cce-admin"
$User1Auth = "Bearer dev:cce-expert"
$User2Auth = "Bearer dev:cce-user"

# Shared state
$Calls         = [System.Collections.Generic.List[pscustomobject]]::new()
$Gaps          = [System.Collections.Generic.List[pscustomobject]]::new()
$Script:Phase  = "Init"
$StartTime     = [System.Diagnostics.Stopwatch]::StartNew()

function Write-Phase { param([string]$T) $Script:Phase = $T; Write-Host "`n== $T ==" -ForegroundColor Cyan }
function Write-OK    { param([string]$T) Write-Host "  OK  $T" -ForegroundColor Green }
function Write-Warn  { param([string]$T) Write-Host "  !!  $T" -ForegroundColor Yellow }
function Write-Fail  { param([string]$T) Write-Host "  XX  $T" -ForegroundColor Red }

function Invoke-Api {
    param(
        [string]$Label,
        [string]$Method,
        [string]$Path,
        [hashtable]$Body   = $null,
        [string]$Auth      = $null,
        [switch]$Internal
    )
    $base    = if ($Internal) { $IntBase } else { $ExtBase }
    $url     = "$base$Path"
    $headers = @{ "Accept" = "application/json"; "Content-Type" = "application/json" }
    if ($Auth) { $headers["Authorization"] = $Auth }

    $sw         = [System.Diagnostics.Stopwatch]::StartNew()
    $statusCode = 0
    $success    = $false
    $errMsg     = $null
    $resp       = $null
    try {
        $splat = @{ Method = $Method; Uri = $url; Headers = $headers; ErrorAction = "Stop" }
        if ($Body) { $splat["Body"] = ($Body | ConvertTo-Json -Depth 10 -Compress) }
        $resp       = Invoke-RestMethod @splat
        $statusCode = 200
        $success    = $true
        Write-OK "$Label  [$($sw.ElapsedMilliseconds)ms]"
    } catch {
        $sw.Stop()
        $statusCode = 0
        if ($_.Exception.Response) { $statusCode = [int]$_.Exception.Response.StatusCode }
        $errMsg = ($_.Exception.Message -replace "`r?`n", " ").Substring(0, [Math]::Min(120, $_.Exception.Message.Length))
        Write-Fail "$Label  [$($sw.ElapsedMilliseconds)ms]  status=$statusCode  $errMsg"
    }
    $sw.Stop()

    $Calls.Add([pscustomobject]@{
        Phase  = $Script:Phase
        Label  = $Label
        Method = $Method
        Path   = $Path
        Ms     = $sw.ElapsedMilliseconds
        Status = $statusCode
        OK     = $success
        Err    = $errMsg
    })
    return $resp
}

function Assert-Counter {
    param([string]$Name, [int]$Expected, [int]$Actual)
    if ($Actual -eq $Expected) {
        Write-OK "Counter ${Name} = $Actual"
    } else {
        Write-Warn "MISMATCH counter ${Name}: expected=$Expected actual=$Actual"
        $Gaps.Add([pscustomobject]@{
            Type     = "Counter"
            Label    = $Name
            Expected = $Expected
            Actual   = $Actual
            Note     = "Denormalized counter lagged - async event not yet processed"
        })
    }
}

function Add-Gap {
    param([string]$Label, [string]$Expected, [string]$Actual, [string]$Note)
    Write-Warn "GAP  $Label  expected=$Expected  actual=$Actual"
    $Gaps.Add([pscustomobject]@{
        Type     = "Gap"
        Label    = $Label
        Expected = $Expected
        Actual   = $Actual
        Note     = $Note
    })
}

# ─────────────────────────────────────────────────────────────────────────────
# PHASE 0 - Health
# ─────────────────────────────────────────────────────────────────────────────
Write-Phase "0 - Health"

$healthChecks = @(
    @{ Base = $ExtBase; Name = "External"; Path = "/api/community/feed?page=1&pageSize=1&sort=1" },
    @{ Base = $IntBase; Name = "Internal"; Path = "/api/admin/community/posts?page=1&pageSize=1" }
)
foreach ($hc in $healthChecks) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $null = Invoke-RestMethod -Uri "$($hc.Base)$($hc.Path)" -Method GET `
            -Headers @{ Authorization = $AdminAuth } -ErrorAction Stop
        $sw.Stop()
        Write-OK "$($hc.Name) API up  [$($sw.ElapsedMilliseconds)ms]"
        $Calls.Add([pscustomobject]@{ Phase="0 - Health"; Label="Health $($hc.Name)"; Method="GET"; Path=$hc.Path; Ms=$sw.ElapsedMilliseconds; Status=200; OK=$true; Err=$null })
    } catch {
        $sw.Stop()
        Write-Fail "$($hc.Name) API unreachable at $($hc.Base)"
        $Calls.Add([pscustomobject]@{ Phase="0 - Health"; Label="Health $($hc.Name)"; Method="GET"; Path=$hc.Path; Ms=$sw.ElapsedMilliseconds; Status=0; OK=$false; Err=$_.Exception.Message })
        if ($hc.Name -eq "External") { Write-Fail "Cannot continue without External API."; exit 1 }
    }
}

# ─────────────────────────────────────────────────────────────────────────────
# PHASE 1 - Discover topicId
# ─────────────────────────────────────────────────────────────────────────────
Write-Phase "1 - Discover topicId"

$topicId = $null

$feedResp1 = Invoke-Api "Global feed Newest p1" "GET" "/api/community/feed?sort=1&page=1&pageSize=10"
$feedItems1 = $null
if ($feedResp1 -and $feedResp1.data -and $feedResp1.data.items) { $feedItems1 = $feedResp1.data.items }
if ($feedItems1 -and $feedItems1.Count -gt 0) { $topicId = $feedItems1[0].topicId }

if (-not $topicId) {
    $feedResp2  = Invoke-Api "Global feed Hot p1" "GET" "/api/community/feed?sort=0&page=1&pageSize=10"
    $feedItems2 = $null
    if ($feedResp2 -and $feedResp2.data -and $feedResp2.data.items) { $feedItems2 = $feedResp2.data.items }
    if ($feedItems2 -and $feedItems2.Count -gt 0) { $topicId = $feedItems2[0].topicId }
}

# Try community-scoped feed when CommunityId is provided
if (-not $topicId -and $CommunityId) {
    $feedResp3  = Invoke-Api "Community feed Newest p1" "GET" "/api/community/feed?communityId=$CommunityId&sort=1&page=1&pageSize=10"
    $feedItems3 = $null
    if ($feedResp3 -and $feedResp3.data -and $feedResp3.data.items) { $feedItems3 = $feedResp3.data.items }
    if ($feedItems3 -and $feedItems3.Count -gt 0) { $topicId = $feedItems3[0].topicId }
}

if (-not $topicId) {
    Write-Fail "No topicId found in feed - run the seeder first: dotnet run --project src/CCE.Seeder -- --demo"
    exit 1
}
Write-OK "TopicId: $topicId"

# ─────────────────────────────────────────────────────────────────────────────
# PHASE 2 - Community setup
# ─────────────────────────────────────────────────────────────────────────────
Write-Phase "2 - Community setup"

$communityId = $null

if ($CommunityId) {
    Write-OK "Using existing community: $CommunityId  (skipping creation)"
    $communityId = $CommunityId
} else {
    $slug       = "test-cycle-$(Get-Date -Format 'yyyyMMddHHmmss')"
    $createResp = Invoke-Api "Create community" "POST" "/api/admin/community/communities" `
        -Body @{
            nameAr        = "Test Community"
            nameEn        = "Automated Test Community"
            descriptionAr = "Temp community for cycle test"
            descriptionEn = "Temporary community for full-cycle testing"
            slug          = $slug
            visibility    = 0
        } -Auth $AdminAuth -Internal
    if ($createResp -and $createResp.data) { $communityId = $createResp.data }

    if (-not $communityId) {
        Write-Fail "Community creation failed - check Internal API logs."
        exit 1
    }
    Write-OK "CommunityId: $communityId  (slug: $slug)"
}

# Both users join (required for posting) then follow (idempotent — 409 on re-join is expected)
$null = Invoke-Api "User1 joins community"   "POST" "/api/community/communities/$communityId/join" -Auth $User1Auth
$null = Invoke-Api "User2 joins community"   "POST" "/api/community/communities/$communityId/join" -Auth $User2Auth
$null = Invoke-Api "User1 follows community" "PUT"  "/api/community/communities/$communityId/follow" `
    -Body @{ status = 0 } -Auth $User1Auth
$null = Invoke-Api "User2 follows community" "PUT"  "/api/community/communities/$communityId/follow" `
    -Body @{ status = 0 } -Auth $User2Auth

# ─────────────────────────────────────────────────────────────────────────────
# PHASE 3 - Create post
# ─────────────────────────────────────────────────────────────────────────────
Write-Phase "3 - Create post"

$ts       = Get-Date -Format "HH:mm:ss"
$postResp = Invoke-Api "Create post (User1)" "POST" "/api/community/posts" `
    -Body @{
        communityId      = $communityId
        topicId          = $topicId
        type             = 0
        title            = "Cycle test post @ $ts"
        content          = "This post exercises the full vote -> comment -> delete -> notification cycle."
        locale           = "en"
        saveAsDraft      = $false
        mentionedUserIds = @()
        tagIds           = @()
    } -Auth $User1Auth

$postId = $null
if ($postResp -and $postResp.data) { $postId = $postResp.data }

if (-not $postId) { Write-Fail "Post creation failed - cannot continue."; exit 1 }
Write-OK "PostId: $postId"

Start-Sleep -Milliseconds 300

$p0       = Invoke-Api "Get post initial state" "GET" "/api/community/posts/$postId"
$upvote0  = 0; $down0 = 0; $comment0 = 0
if ($p0 -and $p0.data) {
    $upvote0  = IntOrZero ($p0.data.upvoteCount)
    $down0    = IntOrZero ($p0.data.downvoteCount)
    $comment0 = IntOrZero ($p0.data.commentsCount)
}
Assert-Counter "Initial UpvoteCount"   0 $upvote0
Assert-Counter "Initial DownvoteCount" 0 $down0
Assert-Counter "Initial CommentsCount" 0 $comment0

# ─────────────────────────────────────────────────────────────────────────────
# PHASE 4 - Vote cycle
# ─────────────────────────────────────────────────────────────────────────────
Write-Phase "4 - Vote cycle"

# 4a: upvote
$null = Invoke-Api "User2 upvote +1" "POST" "/api/community/posts/$postId/vote" `
    -Body @{ direction = 1 } -Auth $User2Auth
Start-Sleep -Milliseconds 500
$p1 = Invoke-Api "Get post after upvote" "GET" "/api/community/posts/$postId"
$up1 = 0; if ($p1 -and $p1.data) { $up1 = IntOrZero ($p1.data.upvoteCount) }
Assert-Counter "UpvoteCount after +1" 1 $up1

# 4b: change to downvote
$null = Invoke-Api "User2 change vote to -1" "POST" "/api/community/posts/$postId/vote" `
    -Body @{ direction = -1 } -Auth $User2Auth
Start-Sleep -Milliseconds 500
$p2 = Invoke-Api "Get post after downvote" "GET" "/api/community/posts/$postId"
$up2 = 0; $down2 = 0
if ($p2 -and $p2.data) { $up2 = IntOrZero ($p2.data.upvoteCount); $down2 = IntOrZero ($p2.data.downvoteCount) }
Assert-Counter "UpvoteCount after flip"   0 $up2
Assert-Counter "DownvoteCount after flip" 1 $down2

# 4c: remove vote
$null = Invoke-Api "User2 remove vote 0" "POST" "/api/community/posts/$postId/vote" `
    -Body @{ direction = 0 } -Auth $User2Auth
Start-Sleep -Milliseconds 500
$p3 = Invoke-Api "Get post after vote removed" "GET" "/api/community/posts/$postId"
$down3 = 0; if ($p3 -and $p3.data) { $down3 = IntOrZero ($p3.data.downvoteCount) }
Assert-Counter "DownvoteCount after removal" 0 $down3

# 4d: final upvote (leaves post at +1)
$null = Invoke-Api "User2 final upvote +1" "POST" "/api/community/posts/$postId/vote" `
    -Body @{ direction = 1 } -Auth $User2Auth
Start-Sleep -Milliseconds 500
$p4 = Invoke-Api "Get post final vote state" "GET" "/api/community/posts/$postId"
$up4 = 0; if ($p4 -and $p4.data) { $up4 = IntOrZero ($p4.data.upvoteCount) }
Assert-Counter "UpvoteCount end of vote cycle" 1 $up4

# ─────────────────────────────────────────────────────────────────────────────
# PHASE 5 - Comment cycle
# ─────────────────────────────────────────────────────────────────────────────
Write-Phase "5 - Comment cycle"

# 5a: User2 reply #1
$r1Resp   = Invoke-Api "User2 adds reply 1" "POST" "/api/community/posts/$postId/replies" `
    -Body @{ content = "Great post! First reply from user2."; locale = "en"; mentionedUserIds = @() } `
    -Auth $User2Auth
$reply1Id = $null
if ($r1Resp -and $r1Resp.data) { $reply1Id = $r1Resp.data }
Start-Sleep -Milliseconds 500
$p5       = Invoke-Api "Get post after reply 1" "GET" "/api/community/posts/$postId"
$comment5 = 0; if ($p5 -and $p5.data) { $comment5 = IntOrZero ($p5.data.commentsCount) }
Assert-Counter "CommentsCount after reply 1" 1 $comment5

# 5b: User1 reply #2
$r2Resp   = Invoke-Api "User1 adds reply 2" "POST" "/api/community/posts/$postId/replies" `
    -Body @{ content = "Thanks for the reply! Follow-up from User1."; locale = "en"; mentionedUserIds = @() } `
    -Auth $User1Auth
$reply2Id = $null
if ($r2Resp -and $r2Resp.data) { $reply2Id = $r2Resp.data }
Start-Sleep -Milliseconds 500
$p6       = Invoke-Api "Get post after reply 2" "GET" "/api/community/posts/$postId"
$comment6 = 0; if ($p6 -and $p6.data) { $comment6 = IntOrZero ($p6.data.commentsCount) }
Assert-Counter "CommentsCount after reply 2" 2 $comment6

# 5c: Verify reply list
$replyList  = Invoke-Api "List replies p1" "GET" "/api/community/posts/$postId/replies?page=1&pageSize=20"
$listedCnt  = 0
if ($replyList -and $replyList.data) {
    if ($replyList.data.items) { $listedCnt = $replyList.data.items.Count }
    elseif ($replyList.data.total) { $listedCnt = [int]$replyList.data.total }
}
if ($listedCnt -ge 2) {
    Write-OK "Reply list returned $listedCnt replies"
} else {
    Add-Gap "Reply list count" ">=2" "$listedCnt" "Reply list returned fewer items than CommentsCount"
}

# 5d: User1 upvotes reply #1
if ($reply1Id) {
    $null = Invoke-Api "User1 upvotes reply 1" "POST" "/api/community/replies/$reply1Id/vote" `
        -Body @{ direction = 1 } -Auth $User1Auth
}

# ─────────────────────────────────────────────────────────────────────────────
# PHASE 6 - Feed verification
# ─────────────────────────────────────────────────────────────────────────────
Write-Phase "6 - Feed verification"

Write-Host "  Waiting 3s for Redis fan-out..." -ForegroundColor DarkGray
Start-Sleep -Seconds 3

# 6a: Community Hot feed
$hotFeed  = Invoke-Api "Community feed Hot p1" "GET" "/api/community/feed?communityId=$communityId&sort=0&page=1&pageSize=20"
$hotPost  = $null
if ($hotFeed -and $hotFeed.data -and $hotFeed.data.items) {
    $hotPost = $hotFeed.data.items | Where-Object { $_.id -eq $postId } | Select-Object -First 1
}
if ($hotPost) {
    Write-OK "Post found in Hot feed"
    $feedUp  = IntOrZero ($hotPost.upvoteCount)
    $feedCmt = IntOrZero ($hotPost.commentsCount)
    Assert-Counter "Feed Hot UpvoteCount"   1 $feedUp
    Assert-Counter "Feed Hot CommentsCount" 2 $feedCmt
} else {
    Add-Gap "Hot feed post visibility" "present" "absent" `
        "Post not in Hot feed after 3s - Redis may be cold or FeedConsumer lagged"
}

# 6b: Community Newest feed
$newFeed  = Invoke-Api "Community feed Newest p1" "GET" "/api/community/feed?communityId=$communityId&sort=1&page=1&pageSize=20"
$newPost  = $null
if ($newFeed -and $newFeed.data -and $newFeed.data.items) {
    $newPost = $newFeed.data.items | Where-Object { $_.id -eq $postId } | Select-Object -First 1
}
if ($newPost) { Write-OK "Post found in Newest feed" } else {
    Add-Gap "Newest feed post visibility" "present" "absent" "Post not in Newest feed"
}

# 6c: Topic-filtered feed
$topicFeed = Invoke-Api "Community feed topic filter" "GET" "/api/community/feed?communityId=$communityId&topicId=$topicId&sort=0&page=1&pageSize=20"
$topicPost = $null
if ($topicFeed -and $topicFeed.data -and $topicFeed.data.items) {
    $topicPost = $topicFeed.data.items | Where-Object { $_.id -eq $postId } | Select-Object -First 1
}
if ($topicPost) { Write-OK "Post found in topic-filtered feed" } else {
    Write-Warn "Post not in topic-filtered feed (over-fetch window may need widening)"
}

# 6d: Personal feed (User1)
$myFeed = Invoke-Api "User1 personal feed Newest" "GET" "/api/me/feed?sort=1&page=1&pageSize=20" -Auth $User1Auth
$myTotal = 0
if ($myFeed -and $myFeed.data) { $myTotal = if ($null -ne $myFeed.data.total) { [int]$myFeed.data.total } else { IntOrZero $myFeed.data.items } }
Write-OK "User1 personal feed total: $myTotal"

# ─────────────────────────────────────────────────────────────────────────────
# PHASE 7 - Notifications
# ─────────────────────────────────────────────────────────────────────────────
Write-Phase "7 - Notifications"

Start-Sleep -Seconds 1

$unreadBefore = Invoke-Api "User1 unread count before" "GET" "/api/me/notifications/unread-count" -Auth $User1Auth
$cntBefore    = 0
if ($unreadBefore -and $null -ne $unreadBefore.data) { $cntBefore = [int]$unreadBefore.data }
Write-OK "User1 unread before: $cntBefore"

$notifPage  = Invoke-Api "User1 notifications p1" "GET" "/api/me/notifications?page=1&pageSize=20" -Auth $User1Auth
$notifItems = @()
if ($notifPage -and $notifPage.data -and $notifPage.data.items) { $notifItems = $notifPage.data.items }
$notifTotal = $notifItems.Count
Write-OK "User1 notifications listed: $notifTotal"

if ($notifItems.Count -gt 0) {
    $firstId = $notifItems[0].id
    $null    = Invoke-Api "Mark 1st notification read" "POST" "/api/me/notifications/$firstId/mark-read" -Auth $User1Auth
    Start-Sleep -Milliseconds 400
    $afterOne = Invoke-Api "User1 unread after mark-one" "GET" "/api/me/notifications/unread-count" -Auth $User1Auth
    $cntAfterOne = 0
    if ($afterOne -and $null -ne $afterOne.data) { $cntAfterOne = [int]$afterOne.data }
    if ($cntAfterOne -lt $cntBefore) {
        Write-OK "Unread decreased: $cntBefore -> $cntAfterOne"
    } else {
        Add-Gap "mark-read counter" "$($cntBefore - 1)" "$cntAfterOne" "Unread count did not decrease after mark-read"
    }

    $null = Invoke-Api "Mark all notifications read" "POST" "/api/me/notifications/mark-all-read" -Auth $User1Auth
    Start-Sleep -Milliseconds 400
    $finalUnread = Invoke-Api "User1 unread after mark-all" "GET" "/api/me/notifications/unread-count" -Auth $User1Auth
    $cntFinal = 0
    if ($finalUnread -and $null -ne $finalUnread.data) { $cntFinal = [int]$finalUnread.data }
    Assert-Counter "Unread after mark-all-read" 0 $cntFinal
} else {
    Add-Gap "Notification delivery" ">0 notifications" "0" `
        "User1 got no notifications - verify MassTransit InMemory consumers are registered in External API startup"
}

# ─────────────────────────────────────────────────────────────────────────────
# PHASE 8 - Delete cycle
# ─────────────────────────────────────────────────────────────────────────────
Write-Phase "8 - Delete cycle"

# 8a: Soft-delete reply #1
if ($reply1Id) {
    $null     = Invoke-Api "Admin soft-deletes reply 1" "DELETE" "/api/admin/community/replies/$reply1Id" `
        -Auth $AdminAuth -Internal
    Start-Sleep -Milliseconds 500
    $p8       = Invoke-Api "Get post after reply 1 deleted" "GET" "/api/community/posts/$postId"
    $comment8 = 0; if ($p8 -and $p8.data) { $comment8 = IntOrZero ($p8.data.commentsCount) }
    Assert-Counter "CommentsCount after reply 1 deleted" 1 $comment8

    $repAfter  = Invoke-Api "Reply list after delete" "GET" "/api/community/posts/$postId/replies?page=1&pageSize=20"
    $repCntAfter = 0
    if ($repAfter -and $repAfter.data -and $repAfter.data.items) { $repCntAfter = $repAfter.data.items.Count }
    elseif ($repAfter -and $repAfter.data -and $repAfter.data.total) { $repCntAfter = [int]$repAfter.data.total }
    if ($repCntAfter -eq 1) {
        Write-OK "Reply list shows 1 reply after soft-delete"
    } else {
        Add-Gap "Reply list after soft-delete" "1" "$repCntAfter" "Soft-deleted reply still appears or count wrong"
    }
}

# 8b: Soft-delete the post
$null = Invoke-Api "Admin soft-deletes post" "DELETE" "/api/admin/community/posts/$postId" `
    -Auth $AdminAuth -Internal
Start-Sleep -Milliseconds 500
$p9 = Invoke-Api "Get post after soft-delete" "GET" "/api/community/posts/$postId"
$stillVisible = ($null -ne $p9 -and $null -ne $p9.data -and $null -ne $p9.data.id)
if (-not $stillVisible) {
    Write-OK "Post not visible after soft-delete"
} else {
    Add-Gap "Soft-delete visibility" "404 / not found" "still visible" `
        "Post returned data after soft-delete - check SoftDeletePostCommandHandler"
}

# ─────────────────────────────────────────────────────────────────────────────
# PHASE 9 - Feed after delete
# ─────────────────────────────────────────────────────────────────────────────
Write-Phase "9 - Feed after delete"

Start-Sleep -Seconds 2

$feedAfterDel = Invoke-Api "Community feed after post delete" "GET" `
    "/api/community/feed?communityId=$communityId&sort=0&page=1&pageSize=20"
$deletedInFeed = $null
if ($feedAfterDel -and $feedAfterDel.data -and $feedAfterDel.data.items) {
    $deletedInFeed = $feedAfterDel.data.items | Where-Object { $_.id -eq $postId } | Select-Object -First 1
}
if (-not $deletedInFeed) {
    Write-OK "Deleted post absent from feed"
} else {
    Add-Gap "Feed stale post after delete" "absent" "present" `
        "Soft-deleted post still in Hot feed - RemovePostFromAllFeedsAsync may not have evicted the Redis key"
}

# ─────────────────────────────────────────────────────────────────────────────
# REPORT
# ─────────────────────────────────────────────────────────────────────────────
Write-Phase "Report"
$StartTime.Stop()
$totalMs = $StartTime.ElapsedMilliseconds

$okCalls   = ($Calls | Where-Object { $_.OK }).Count
$failCalls = ($Calls | Where-Object { -not $_.OK }).Count
$allMs     = @($Calls | Select-Object -ExpandProperty Ms)
$avgMs     = if ($allMs.Count) { [int](($allMs | Measure-Object -Average).Average) } else { 0 }
$sortedMs  = $allMs | Sort-Object
$p50Ms     = if ($sortedMs.Count) { $sortedMs[[int]($sortedMs.Count * 0.5)] } else { 0 }
$p95Ms     = if ($sortedMs.Count) { $sortedMs[[Math]::Min([int]($sortedMs.Count * 0.95), $sortedMs.Count - 1)] } else { 0 }
$maxMs     = if ($allMs.Count) { [int](($allMs | Measure-Object -Maximum).Maximum) } else { 0 }

$phaseNames = @($Calls | Select-Object -ExpandProperty Phase | Select-Object -Unique)
$phaseRows  = foreach ($ph in $phaseNames) {
    $pc   = @($Calls | Where-Object { $_.Phase -eq $ph })
    $pMs  = @($pc | Select-Object -ExpandProperty Ms)
    [pscustomobject]@{
        Phase = $ph
        Calls = $pc.Count
        OK    = ($pc | Where-Object { $_.OK }).Count
        AvgMs = if ($pMs.Count) { [int](($pMs | Measure-Object -Average).Average) } else { 0 }
        MaxMs = if ($pMs.Count) { [int](($pMs | Measure-Object -Maximum).Maximum) } else { 0 }
    }
}

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add("# Community Cycle Test Report")
$lines.Add("")
$lines.Add("**Date:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
$lines.Add("**Duration:** $([Math]::Round($totalMs / 1000, 1))s")
$lines.Add("**External API:** $ExtBase")
$lines.Add("**Internal API:** $IntBase")
$lines.Add("**Community ID:** $communityId")
$lines.Add("")
$lines.Add("---")
$lines.Add("")
$lines.Add("## Summary")
$lines.Add("")
$lines.Add("| Metric | Value |")
$lines.Add("|--------|-------|")
$lines.Add("| Total API calls | $($Calls.Count) |")
$lines.Add("| Succeeded | $okCalls |")
$lines.Add("| Failed | $failCalls |")
$lines.Add("| Gaps detected | $($Gaps.Count) |")
$lines.Add("| Avg response | ${avgMs}ms |")
$lines.Add("| p50 | ${p50Ms}ms |")
$lines.Add("| p95 | ${p95Ms}ms |")
$lines.Add("| Max | ${maxMs}ms |")
$lines.Add("")
$lines.Add("---")
$lines.Add("")
$lines.Add("## Response Times by Phase")
$lines.Add("")
$lines.Add("| Phase | Calls | OK | Avg ms | Max ms |")
$lines.Add("|-------|-------|----|--------|--------|")
foreach ($r in $phaseRows) {
    $lines.Add("| $($r.Phase) | $($r.Calls) | $($r.OK) | $($r.AvgMs) | $($r.MaxMs) |")
}
$lines.Add("")
$lines.Add("---")
$lines.Add("")
$lines.Add("## Full Call Log")
$lines.Add("")
$lines.Add("| # | Phase | Label | Method | Status | ms |")
$lines.Add("|---|-------|-------|--------|--------|----|")
$i = 1
foreach ($c in $Calls) {
    $st = if ($c.OK) { "OK" } else { "FAIL $($c.Status)" }
    $lines.Add("| $i | $($c.Phase) | $($c.Label) | $($c.Method) | $st | $($c.Ms) |")
    $i++
}
$lines.Add("")
$lines.Add("---")
$lines.Add("")
$lines.Add("## Gaps and Anomalies")
$lines.Add("")
if ($Gaps.Count -eq 0) {
    $lines.Add("> No gaps detected - all counters, feeds, and notifications matched expected values.")
} else {
    $lines.Add("| Type | Label | Expected | Actual | Note |")
    $lines.Add("|------|-------|----------|--------|------|")
    foreach ($g in $Gaps) {
        $lines.Add("| $($g.Type) | $($g.Label) | $($g.Expected) | $($g.Actual) | $($g.Note) |")
    }
}
$lines.Add("")
$lines.Add("---")
$lines.Add("")
$lines.Add("## Observations")
$lines.Add("")

$obs = [System.Collections.Generic.List[string]]::new()

if ($p95Ms -le 150) {
    $obs.Add("- **p95 ${p95Ms}ms - excellent.** Redis fast-path is serving feed calls.")
} elseif ($p95Ms -le 400) {
    $obs.Add("- **p95 ${p95Ms}ms - acceptable.** Cold Redis will hydrate from SQL on first call and warm up thereafter.")
} else {
    $obs.Add("- **p95 ${p95Ms}ms - investigate.** Above 400ms suggests missing indexes or Redis miss forcing full SQL scans.")
}

$feedCalls = @($Calls | Where-Object { $_.Label -match "feed" })
if ($feedCalls.Count -gt 0) {
    $feedAvg = [int](($feedCalls | Measure-Object Ms -Average).Average)
    $feedMax = [int](($feedCalls | Measure-Object Ms -Maximum).Maximum)
    if ($feedAvg -gt 300) {
        $obs.Add("- **Feed avg ${feedAvg}ms (max ${feedMax}ms):** Cold Redis - first call falls to SQL. Subsequent calls should be faster once feed keys are populated by FeedConsumer/VoteConsumer.")
    } else {
        $obs.Add("- **Feed avg ${feedAvg}ms (max ${feedMax}ms):** Redis fast-path is active.")
    }
}

$notifGap = @($Gaps | Where-Object { $_.Label -match "Notification delivery" })
if ($notifGap.Count -gt 0) {
    $obs.Add("- **Notification gap:** Check that MassTransit consumers are registered in CCE.Api.External startup. In dev with InMemory transport the handler runs on a background thread; increasing the wait delay may help. Also query: SELECT * FROM outbox_message WHERE sent_time IS NULL")
}

$feedDelGap = @($Gaps | Where-Object { $_.Label -match "stale post" })
if ($feedDelGap.Count -gt 0) {
    $obs.Add("- **Stale feed after delete:** SoftDeletePostCommandHandler calls RemovePostFromAllFeedsAsync. If post is still in feed: (a) Redis not connected so eviction skipped, (b) hot leaderboard key not evicted, or (c) HydrateAsync visibility guard not firing (check PostStatus.Published filter).")
}

$counterGaps = @($Gaps | Where-Object { $_.Type -eq "Counter" })
if ($counterGaps.Count -gt 0) {
    $obs.Add("- **Counter mismatches ($($counterGaps.Count)):** Vote/comment counters are denormalized. Check DomainEventDispatcher interceptor and MassTransit consumer processing.")
}

if ($failCalls -gt 0) {
    $failedLabels = ($Calls | Where-Object { -not $_.OK } | Select-Object -ExpandProperty Label) -join ", "
    $obs.Add("- **$failCalls failed call(s):** $failedLabels")
}

if ($obs.Count -eq 0) { $obs.Add("- All phases completed cleanly with no gaps or anomalies.") }
foreach ($o in $obs) { $lines.Add($o) }

$lines.Add("")
$lines.Add("---")
$lines.Add("*Generated by test-community-cycle.ps1*")

[System.IO.File]::WriteAllLines($ReportPath, $lines, [System.Text.Encoding]::UTF8)
Write-OK "Report written -> $ReportPath"

# Console summary
Write-Host ""
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host "  Calls: $($Calls.Count)   OK: $okCalls   Fail: $failCalls" -ForegroundColor White
Write-Host "  avg ${avgMs}ms  p50 ${p50Ms}ms  p95 ${p95Ms}ms  max ${maxMs}ms" -ForegroundColor White
if ($Gaps.Count -gt 0) {
    Write-Host "  Gaps: $($Gaps.Count)" -ForegroundColor Yellow
    foreach ($g in $Gaps) { Write-Host "    - $($g.Label)" -ForegroundColor Yellow }
} else {
    Write-Host "  Gaps: 0  -- clean run" -ForegroundColor Green
}
Write-Host "=======================================" -ForegroundColor Cyan
