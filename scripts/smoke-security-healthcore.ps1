param(
    [string]$BaseUrl = "http://localhost:5230",
    [switch]$CreatePatientIfMissing
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Net.Http

$BaseUrl = $BaseUrl.TrimEnd("/")
$httpClient = [System.Net.Http.HttpClient]::new()
$httpClient.Timeout = [TimeSpan]::FromSeconds(30)
$correlationPrefix = "security-smoke-$([guid]::NewGuid())"

function Write-Pass {
    param([string]$Message)

    Write-Host "  PASS $Message" -ForegroundColor Green
}

function Write-Skip {
    param([string]$Message)

    Write-Host "  SKIP $Message" -ForegroundColor Yellow
}

function Write-Fail {
    param([string]$Message)

    Write-Host "  FAIL $Message" -ForegroundColor Red
}

function Confirm-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }

    Write-Pass $Message
}

function Get-Items {
    param([object]$Value)

    if ($null -eq $Value) {
        return @()
    }

    if ($Value -is [System.Array]) {
        return @($Value)
    }

    return @($Value)
}

function New-AllowedHeaders {
    return @{
        "X-HealthCore-Product" = "InternalAdmin"
        "X-HealthCore-Product-Role" = "HealthCoreAdmin"
        "X-HealthCore-Service-Account" = "dev-admin"
        "X-Correlation-ID" = "$correlationPrefix-allowed"
    }
}

function New-DeniedHeaders {
    return @{
        "X-HealthCore-Product" = "UnknownProduct"
        "X-HealthCore-Product-Role" = "UnknownRole"
        "X-HealthCore-Service-Account" = "denied-smoke"
        "X-Correlation-ID" = "$correlationPrefix-denied"
    }
}

function Invoke-HealthCoreApi {
    param(
        [ValidateSet("GET", "POST", "PUT", "DELETE")]
        [string]$Method,
        [string]$Path,
        [hashtable]$Headers = @{},
        [object]$Body = $null,
        [int[]]$ExpectedStatus = @(200)
    )

    $request = [System.Net.Http.HttpRequestMessage]::new(
        [System.Net.Http.HttpMethod]::new($Method),
        "$BaseUrl$Path")

    $request.Headers.Accept.ParseAdd("application/json")

    foreach ($header in $Headers.GetEnumerator()) {
        $request.Headers.TryAddWithoutValidation($header.Key, [string]$header.Value) | Out-Null
    }

    if ($null -ne $Body) {
        $json = $Body | ConvertTo-Json -Depth 20
        $request.Content = [System.Net.Http.StringContent]::new(
            $json,
            [System.Text.Encoding]::UTF8,
            "application/json")
    }

    $response = $null

    try {
        $response = $httpClient.SendAsync($request).GetAwaiter().GetResult()
        $content = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        $statusCode = [int]$response.StatusCode

        if ($ExpectedStatus -notcontains $statusCode) {
            throw "$Method $Path expected HTTP $($ExpectedStatus -join "/"), got HTTP $statusCode. Response: $content"
        }

        $parsedBody = $null

        if (-not [string]::IsNullOrWhiteSpace($content)) {
            try {
                $parsedBody = $content | ConvertFrom-Json
            } catch {
                $parsedBody = $content
            }
        }

        return [pscustomobject]@{
            StatusCode = $statusCode
            Body = $parsedBody
            RawBody = $content
        }
    } finally {
        if ($null -ne $response) {
            $response.Dispose()
        }

        $request.Dispose()
    }
}

function Invoke-Section {
    param(
        [string]$Name,
        [scriptblock]$Action
    )

    Write-Host ""
    Write-Host "== $Name ==" -ForegroundColor Cyan

    try {
        & $Action
        Write-Host "PASS $Name" -ForegroundColor Green
    } catch {
        Write-Fail $Name
        throw
    }
}

function New-SmokePatient {
    $stamp = Get-Date -Format "yyyyMMddHHmmssfff"
    $mobile = "09" + (Get-Random -Minimum 100000000 -Maximum 999999999)

    $response = Invoke-HealthCoreApi `
        -Method POST `
        -Path "/api/health-core/patients" `
        -Headers (New-AllowedHeaders) `
        -ExpectedStatus 201 `
        -Body @{
            firstName = "Security"
            lastName = "Smoke$stamp"
            birthDate = "1990-01-01"
            gender = "Other"
            bloodType = "O+"
            mobileNumber = $mobile
            email = "security.smoke.$stamp@example.test"
        }

    return $response.Body
}

try {
    Write-Host "Health Core local security smoke test"
    Write-Host "BaseUrl: $BaseUrl"
    Write-Host "Correlation prefix: $correlationPrefix"

    $allowedHeaders = New-AllowedHeaders
    $deniedHeaders = New-DeniedHeaders

    Invoke-Section "Health endpoint" {
        $health = Invoke-HealthCoreApi -Method GET -Path "/health"
        Confirm-True ($health.Body.status -eq "Healthy") "/health reports Healthy"
    }

    Invoke-Section "Patient directory authorization" {
        $allowed = Invoke-HealthCoreApi `
            -Method GET `
            -Path "/api/health-core/patients?page=1&pageSize=5" `
            -Headers $allowedHeaders

        $script:Patients = @(Get-Items $allowed.Body)
        Confirm-True ($allowed.StatusCode -eq 200) "InternalAdmin directory request is allowed"

        Invoke-HealthCoreApi `
            -Method GET `
            -Path "/api/health-core/patients?page=1&pageSize=5" `
            -Headers $deniedHeaders `
            -ExpectedStatus 403 | Out-Null

        Write-Pass "Unknown product/role directory request is denied with 403"
    }

    Invoke-Section "Patient-scoped authorization" {
        $patient = $null

        if ($script:Patients.Count -gt 0) {
            $patient = $script:Patients[0]
        } elseif ($CreatePatientIfMissing) {
            $patient = New-SmokePatient
            Write-Pass "Created local security-smoke patient because none existed"
        }

        if ($null -eq $patient -or [string]::IsNullOrWhiteSpace("$($patient.id)")) {
            Write-Skip "No patient exists. Re-run with -CreatePatientIfMissing or run .\scripts\smoke-healthcore.ps1 first."
            return
        }

        $patientId = $patient.id
        Write-Host "  Using patient id: $patientId"

        Invoke-HealthCoreApi `
            -Method GET `
            -Path "/api/health-core/patients/$patientId/summary" `
            -Headers $allowedHeaders | Out-Null

        Write-Pass "InternalAdmin patient summary request is allowed"

        Invoke-HealthCoreApi `
            -Method GET `
            -Path "/api/health-core/patients/$patientId/summary" `
            -Headers $deniedHeaders `
            -ExpectedStatus 403 | Out-Null

        Write-Pass "Unknown product/role patient summary request is denied with 403"

        Invoke-HealthCoreApi `
            -Method GET `
            -Path "/api/health-core/patients/$patientId/documents" `
            -Headers $allowedHeaders | Out-Null

        Write-Pass "InternalAdmin patient documents request is allowed"

        Invoke-HealthCoreApi `
            -Method GET `
            -Path "/api/health-core/patients/$patientId/documents" `
            -Headers $deniedHeaders `
            -ExpectedStatus 403 | Out-Null

        Write-Pass "Unknown product/role patient documents request is denied with 403"
    }

    Write-Host ""
    Write-Host "Security smoke test completed successfully." -ForegroundColor Green
    Write-Host "Correlation prefix: $correlationPrefix"
    Write-Host "AuditLog verification is not performed by this script. Use docs/security/security-smoke-test-plan.md for local SQL options."

    exit 0
} catch {
    Write-Host ""
    Write-Host "Security smoke test failed." -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red

    exit 1
} finally {
    $httpClient.Dispose()
}
