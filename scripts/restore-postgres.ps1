param(
    [Parameter(Mandatory = $true)]
    [string]$BackupFile,
    [string]$ContainerName = "zibzie-healthcore-db",
    [string]$DatabaseName = "zibzie_healthcore",
    [string]$Username = "zibzie",
    [switch]$UseLocalPgRestore,
    [string]$DbHost = "localhost",
    [int]$DbPort = 5432,
    [switch]$Force
)

$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)

    Write-Host "== $Message ==" -ForegroundColor Cyan
}

function Fail {
    param([string]$Message)

    throw $Message
}

function Invoke-Checked {
    param(
        [string]$FilePath,
        [string[]]$Arguments,
        [string]$FailureMessage
    )

    & $FilePath @Arguments

    if ($LASTEXITCODE -ne 0) {
        Fail $FailureMessage
    }
}

function Confirm-CommandExists {
    param([string]$CommandName)

    if ($null -eq (Get-Command $CommandName -ErrorAction SilentlyContinue)) {
        Fail "Required command '$CommandName' was not found."
    }
}

function Confirm-DockerContainerRunning {
    param([string]$Name)

    Confirm-CommandExists "docker"

    $running = & docker inspect --format "{{.State.Running}}" $Name 2>$null

    if ($LASTEXITCODE -ne 0 -or $running -ne "true") {
        Fail "Docker container '$Name' is not running or is unavailable."
    }
}

function Confirm-RestoreIntent {
    param(
        [string]$Database,
        [switch]$ForceRestore
    )

    Write-Warning "This restore can overwrite or remove data in database '$Database'."
    Write-Warning "Use only against local/dev or an approved restore-test environment."

    if ($ForceRestore) {
        return
    }

    $confirmation = Read-Host "Type RESTORE to continue"

    if ($confirmation -ne "RESTORE") {
        Fail "Restore cancelled."
    }
}

$resolvedBackupFile = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($BackupFile)

if (-not (Test-Path -LiteralPath $resolvedBackupFile)) {
    Fail "Backup file '$resolvedBackupFile' was not found."
}

$backupInfo = Get-Item -LiteralPath $resolvedBackupFile

if ($backupInfo.Length -le 0) {
    Fail "Backup file '$resolvedBackupFile' is empty."
}

Write-Host "Health Core PostgreSQL restore"
Write-Host "Database: $DatabaseName"
Write-Host "Backup: $resolvedBackupFile"
Write-Host "Reminder: backup files contain sensitive health data. Handle restored data securely."

Confirm-RestoreIntent -Database $DatabaseName -ForceRestore:$Force

if ($UseLocalPgRestore) {
    Write-Step "Running local pg_restore"
    Confirm-CommandExists "pg_restore"

    $arguments = @(
        "--host", $DbHost,
        "--port", "$DbPort",
        "--username", $Username,
        "--dbname", $DatabaseName,
        "--clean",
        "--if-exists",
        "--no-owner",
        "--no-privileges",
        $resolvedBackupFile
    )

    Invoke-Checked `
        -FilePath "pg_restore" `
        -Arguments $arguments `
        -FailureMessage "Local pg_restore failed. If password authentication is required, set PGPASSWORD in your shell or use Docker mode."
} else {
    Write-Step "Running Docker pg_restore"
    Confirm-DockerContainerRunning -Name $ContainerName

    $containerBackupPath = "/tmp/$($backupInfo.Name)"

    try {
        Invoke-Checked `
            -FilePath "docker" `
            -Arguments @(
                "cp",
                $resolvedBackupFile,
                "$ContainerName`:$containerBackupPath"
            ) `
            -FailureMessage "Failed to copy backup into container '$ContainerName'."

        Invoke-Checked `
            -FilePath "docker" `
            -Arguments @(
                "exec",
                $ContainerName,
                "pg_restore",
                "--username", $Username,
                "--dbname", $DatabaseName,
                "--clean",
                "--if-exists",
                "--no-owner",
                "--no-privileges",
                $containerBackupPath
            ) `
            -FailureMessage "Docker pg_restore failed inside container '$ContainerName'."
    } finally {
        & docker exec $ContainerName rm -f $containerBackupPath 2>$null | Out-Null
    }
}

Write-Host "Restore completed successfully." -ForegroundColor Green
