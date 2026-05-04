#requires -Version 7.0
#requires -RunAsAdministrator
<#
.SYNOPSIS
    CCE Sub-10c — register 5 Windows Task Scheduler tasks for backup automation.

.DESCRIPTION
    Registers 5 schtasks to drive Ola Hallengren backup procs on the host's
    SQL Server, plus an hourly off-host robocopy sync. Tasks run as the
    cce-sqlbackup-svc service account (operator provisions; documented in
    backup-restore.md runbook).

    Idempotent — re-running deletes + re-creates each task by name.

.PARAMETER Environment
    Environment name; passed to Sync-OffHost.ps1 invocations.

.PARAMETER EnvFile
    Override env-file path. Default: C:\ProgramData\CCE\.env.<Environment>.

.PARAMETER ServiceAccount
    Account to run the tasks as. Default: NT AUTHORITY\SYSTEM (works for
    SQL local-Windows-auth + filesystem-local writes; UNC sync needs a
    domain account — operator overrides).

.EXAMPLE
    .\infra\backup\Register-ScheduledTasks.ps1 -Environment prod -ServiceAccount cce.local\cce-sqlbackup-svc
#>
[CmdletBinding()]
param(
    [ValidateSet('test','preprod','prod','dr')]
    [string]$Environment = 'prod',
    [string]$EnvFile,
    [string]$ServiceAccount = 'NT AUTHORITY\SYSTEM'
)

$ErrorActionPreference = 'Stop'

if (-not $EnvFile -or $EnvFile -eq '') {
    $EnvFile = "C:\ProgramData\CCE\.env.$Environment"
}

$syncScript = Join-Path $PSScriptRoot 'Sync-OffHost.ps1'

# Backup root + directories.
$backupRoot = 'D:\CCEBackups'
foreach ($sub in @('FULL','DIFF','LOG')) {
    $dir = Join-Path $backupRoot $sub
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
        Write-Host "Created backup dir: $dir"
    }
}

# Helper: build the pwsh command that resolves SQL creds at task-run time
# from .env.<env> (so passwords aren't embedded in task command lines).
function Build-SqlTaskArgument {
    param([string]$Sql)
    $sqlEscaped = $Sql -replace "'", "''"
    return "-NoProfile -Command `"" +
        "`$envFile = '$EnvFile'; " +
        "`$envMap = @{}; foreach (`$ln in Get-Content `$envFile) { if (`$ln -match '^([A-Z_]+)=(.*)$') { `$envMap[`$Matches[1]] = `$Matches[2].Trim() } }; " +
        "`$cs = `$envMap['INFRA_SQL']; " +
        "`$server = ([regex]::Match(`$cs, 'Server=([^;]+)')).Groups[1].Value; " +
        "`$user = ([regex]::Match(`$cs, 'User Id=([^;]+)')).Groups[1].Value; " +
        "`$pwd = ([regex]::Match(`$cs, 'Password=([^;]+)')).Groups[1].Value; " +
        "sqlcmd -S `$server -d master -U `$user -P `$pwd -b -Q `\`"$sqlEscaped`\`"" +
        "`""
}

# Task definitions: name + trigger-builder + action.
$tasks = @(
    @{
        Name = 'CCE-Backup-Full'
        TriggerBuilder = { New-ScheduledTaskTrigger -Daily -At '02:00' }
        ActionArg = (Build-SqlTaskArgument "EXEC dbo.DatabaseBackup @Databases='USER_DATABASES', @Directory='D:\CCEBackups\FULL', @BackupType='FULL', @Verify='Y', @CleanupTime=168, @Compress='Y'")
    }
    @{
        Name = 'CCE-Backup-Diff'
        TriggerBuilder = {
            New-ScheduledTaskTrigger -Once -At (Get-Date -Hour 0 -Minute 0 -Second 0) `
                -RepetitionInterval (New-TimeSpan -Hours 6) `
                -RepetitionDuration ([System.TimeSpan]::FromDays(36500))
        }
        ActionArg = (Build-SqlTaskArgument "EXEC dbo.DatabaseBackup @Databases='USER_DATABASES', @Directory='D:\CCEBackups\DIFF', @BackupType='DIFF', @Verify='Y', @CleanupTime=168, @Compress='Y'")
    }
    @{
        Name = 'CCE-Backup-Log'
        TriggerBuilder = {
            New-ScheduledTaskTrigger -Once -At (Get-Date -Hour 0 -Minute 0 -Second 0) `
                -RepetitionInterval (New-TimeSpan -Minutes 15) `
                -RepetitionDuration ([System.TimeSpan]::FromDays(36500))
        }
        ActionArg = (Build-SqlTaskArgument "EXEC dbo.DatabaseBackup @Databases='USER_DATABASES', @Directory='D:\CCEBackups\LOG', @BackupType='LOG', @Verify='Y', @CleanupTime=24, @Compress='Y'")
    }
    @{
        Name = 'CCE-Backup-IntegrityCheck'
        TriggerBuilder = { New-ScheduledTaskTrigger -Weekly -DaysOfWeek Sunday -At '03:00' }
        ActionArg = (Build-SqlTaskArgument "EXEC dbo.DatabaseIntegrityCheck @Databases='USER_DATABASES'")
    }
    @{
        Name = 'CCE-Backup-Sync-OffHost'
        TriggerBuilder = {
            New-ScheduledTaskTrigger -Once -At (Get-Date -Hour 0 -Minute 0 -Second 0) `
                -RepetitionInterval (New-TimeSpan -Hours 1) `
                -RepetitionDuration ([System.TimeSpan]::FromDays(36500))
        }
        ActionArg = "-NoProfile -File `"$syncScript`" -Environment $Environment"
    }
)

# Register each task. Idempotent: delete first if exists, then re-register.
foreach ($t in $tasks) {
    $name = $t.Name
    $action = New-ScheduledTaskAction -Execute 'pwsh' -Argument $t.ActionArg
    $trigger = & $t.TriggerBuilder
    $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries `
                                              -StartWhenAvailable -ExecutionTimeLimit (New-TimeSpan -Hours 4)

    if (Get-ScheduledTask -TaskName $name -ErrorAction SilentlyContinue) {
        Write-Host "Task '$name' exists; deleting + re-registering."
        Unregister-ScheduledTask -TaskName $name -Confirm:$false
    }

    Register-ScheduledTask -TaskName $name -Action $action -Trigger $trigger -Settings $settings `
                           -User $ServiceAccount -RunLevel Highest -Force | Out-Null
    Write-Host "Registered task: $name"
}

Write-Host ""
Write-Host "All 5 backup tasks registered. Verify with:"
Write-Host "  Get-ScheduledTask | Where-Object TaskName -like 'CCE-Backup-*'"
exit 0
