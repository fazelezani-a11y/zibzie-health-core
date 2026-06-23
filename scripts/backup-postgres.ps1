param(
    [string]$ContainerName = "zibzie-healthcore-db",
    [string]$DatabaseName = "zibzie_healthcore",
    [string]$Username = "zibzie",
    [string]$OutputDirectory = ".\backups\postgres",
    [switch]$UseLocalPgDump,
    [string]$DbHost = "localhost",
    [int]$DbPort = 5432
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

function New-BackupFilePath {
    param(
        [string]$Directory,
        [string]$Database
    )

    $resolvedDirectory = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Directory)
    New-Item -ItemType Directory -Path $resolvedDirectory -Force | Out-Null

    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $fileName = "$Database-$timestamp.dump"

    return Join-Path $resolvedDirectory $fileName
}

$backupPath = New-BackupFilePath -Directory $OutputDirectory -Database $DatabaseName

Write-Host "Health Core PostgreSQL backup"
Write-Host "Database: $DatabaseName"
Write-Host "Output: $backupPath"
Write-Host "Reminder: backup files contain sensitive health data. Store and transfer them securely."

if ($UseLocalPgDump) {
    Write-Step "Running local pg_dump"
    Confirm-CommandExists "pg_dump"

    $arguments = @(
        "--host", $DbHost,
        "--port", "$DbPort",
        "--username", $Username,
        "--dbname", $DatabaseName,
        "--format", "custom",
        "--no-owner",
        "--no-privileges",
        "--file", $backupPath
    )

    Invoke-Checked `
        -FilePath "pg_dump" `
        -Arguments $arguments `
        -FailureMessage "Local pg_dump failed. If password authentication is required, set PGPASSWORD in your shell or use Docker mode."
} else {
    Write-Step "Running Docker pg_dump"
    Confirm-DockerContainerRunning -Name $ContainerName

    $containerBackupPath = "/tmp/$([System.IO.Path]::GetFileName($backupPath))"

    try {
        Invoke-Checked `
            -FilePath "docker" `
            -Arguments @(
                "exec",
                $ContainerName,
                "pg_dump",
                "--username", $Username,
                "--dbname", $DatabaseName,
                "--format", "custom",
                "--no-owner",
                "--no-privileges",
                "--file", $containerBackupPath
            ) `
            -FailureMessage "Docker pg_dump failed inside container '$ContainerName'."

        Invoke-Checked `
            -FilePath "docker" `
            -Arguments @(
                "cp",
                "$ContainerName`:$containerBackupPath",
                $backupPath
            ) `
            -FailureMessage "Failed to copy backup from container '$ContainerName'."
    } finally {
        & docker exec $ContainerName rm -f $containerBackupPath 2>$null | Out-Null
    }
}

if (-not (Test-Path -LiteralPath $backupPath)) {
    Fail "Backup file was not created."
}

$backupInfo = Get-Item -LiteralPath $backupPath

if ($backupInfo.Length -le 0) {
    Fail "Backup file was created but is empty."
}

Write-Host "Backup completed successfully." -ForegroundColor Green
Write-Host "File: $($backupInfo.FullName)"
Write-Host "Size: $($backupInfo.Length) bytes"
