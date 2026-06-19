param(
    [string]$BaseUrl = "http://localhost:5230"
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Net.Http

$BaseUrl = $BaseUrl.TrimEnd("/")
$httpClient = [System.Net.Http.HttpClient]::new()
$httpClient.Timeout = [TimeSpan]::FromSeconds(30)

function Write-Pass {
    param([string]$Message)

    Write-Host "  PASS $Message" -ForegroundColor Green
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

function Same-Id {
    param(
        [object]$Left,
        [object]$Right
    )

    return "$Left".ToLowerInvariant() -eq "$Right".ToLowerInvariant()
}

function Find-ById {
    param(
        [object]$Items,
        [object]$Id
    )

    return @((Get-Items $Items) | Where-Object { Same-Id $_.id $Id })
}

function Has-RelatedTimelineEvent {
    param(
        [object]$Events,
        [string]$RelatedRecordType,
        [object]$RelatedRecordId
    )

    $matches = @((Get-Items $Events) | Where-Object {
        $_.relatedRecordType -eq $RelatedRecordType -and
        (Same-Id $_.relatedRecordId $RelatedRecordId)
    })

    return $matches.Count -gt 0
}

function Invoke-HealthCoreApi {
    param(
        [ValidateSet("GET", "POST", "PUT", "DELETE")]
        [string]$Method,
        [string]$Path,
        [object]$Body = $null,
        [int[]]$ExpectedStatus = @(200)
    )

    $request = [System.Net.Http.HttpRequestMessage]::new(
        [System.Net.Http.HttpMethod]::new($Method),
        "$BaseUrl$Path")

    $request.Headers.Accept.ParseAdd("application/json")

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

try {
    Write-Host "Health Core local smoke test"
    Write-Host "BaseUrl: $BaseUrl"

    $stamp = Get-Date -Format "yyyyMMddHHmmssfff"
    $mobile = "09" + (Get-Random -Minimum 100000000 -Maximum 999999999)

    Invoke-Section "Health endpoint" {
        $health = Invoke-HealthCoreApi -Method GET -Path "/health"
        Confirm-True ($health.Body.status -eq "Healthy") "/health reports Healthy"
    }

    Invoke-Section "Patient" {
        $patient = Invoke-HealthCoreApi -Method POST -Path "/api/health-core/patients" -ExpectedStatus 201 -Body @{
            firstName = "Smoke"
            lastName = "Patient$stamp"
            birthDate = "1990-01-01"
            gender = "Other"
            bloodType = "O+"
            mobileNumber = $mobile
            email = "smoke.$stamp@example.test"
        }

        $script:Patient = $patient.Body
        $script:PatientId = $patient.Body.id

        $parsedGuid = [guid]::Empty
        Confirm-True ([guid]::TryParse("$script:PatientId", [ref]$parsedGuid)) "patient create returned an id"
        Confirm-True ($patient.Body.mobileNumber -eq $mobile) "patient create returned expected mobile number"

        $patients = Invoke-HealthCoreApi -Method GET -Path "/api/health-core/patients?search=$mobile&page=1&pageSize=20"
        Confirm-True (@(Find-ById $patients.Body $script:PatientId).Count -ge 1) "patient appears in list/search"

        $summary = Invoke-HealthCoreApi -Method GET -Path "/api/health-core/patients/$script:PatientId/summary"
        $script:PatientSummary = $summary.Body
        Confirm-True (Same-Id $summary.Body.id $script:PatientId) "patient summary loads"
    }

    Invoke-Section "Medical history" {
        $condition = Invoke-HealthCoreApi -Method POST -Path "/api/health-core/patients/$script:PatientId/conditions" -ExpectedStatus 201 -Body @{
            name = "Hypertension smoke"
            status = "Active"
            startedYear = 2024
            treatmentSummary = "Smoke test condition"
            sourceType = "Manual"
            verificationStatus = "ClinicianVerified"
            sensitivityLevel = "Normal"
        }

        $allergy = Invoke-HealthCoreApi -Method POST -Path "/api/health-core/patients/$script:PatientId/allergies" -ExpectedStatus 201 -Body @{
            allergen = "Penicillin smoke"
            allergyType = "Drug"
            severity = "Moderate"
            reactionDescription = "Rash"
            sourceType = "Manual"
            verificationStatus = "ClinicianVerified"
            sensitivityLevel = "Normal"
        }

        $medication = Invoke-HealthCoreApi -Method POST -Path "/api/health-core/patients/$script:PatientId/medications" -ExpectedStatus 201 -Body @{
            name = "Metformin smoke"
            dose = "500 mg"
            frequency = "Daily"
            route = "Oral"
            reason = "Smoke test"
            startDate = "2026-06-01"
            isCurrent = $true
            sourceType = "Manual"
            verificationStatus = "ClinicianVerified"
            sensitivityLevel = "Normal"
        }

        $script:ConditionId = $condition.Body.id
        $script:AllergyId = $allergy.Body.id
        $script:MedicationId = $medication.Body.id

        Confirm-True (-not [string]::IsNullOrWhiteSpace("$script:ConditionId")) "condition create returned an id"
        Confirm-True (-not [string]::IsNullOrWhiteSpace("$script:AllergyId")) "allergy create returned an id"
        Confirm-True (-not [string]::IsNullOrWhiteSpace("$script:MedicationId")) "medication create returned an id"

        $summary = Invoke-HealthCoreApi -Method GET -Path "/api/health-core/patients/$script:PatientId/summary"
        Confirm-True (@(Find-ById $summary.Body.conditions $script:ConditionId).Count -eq 1) "condition appears in summary"
        Confirm-True (@(Find-ById $summary.Body.allergies $script:AllergyId).Count -eq 1) "allergy appears in summary"
        Confirm-True (@(Find-ById $summary.Body.currentMedications $script:MedicationId).Count -eq 1) "medication appears in summary"
    }

    Invoke-Section "Timeline" {
        $manualEvent = Invoke-HealthCoreApi -Method POST -Path "/api/health-core/patients/$script:PatientId/timeline" -ExpectedStatus 201 -Body @{
            eventType = "ManualSmoke"
            title = "Manual smoke event"
            description = "Created by smoke test"
            occurredAt = "2026-06-19T08:00:00Z"
            sourceType = "Manual"
            visibility = "Internal"
            sensitivityLevel = "Normal"
        }

        $script:ManualTimelineEventId = $manualEvent.Body.id
        Confirm-True (-not [string]::IsNullOrWhiteSpace("$script:ManualTimelineEventId")) "manual timeline create returned an id"

        $timeline = Invoke-HealthCoreApi -Method GET -Path "/api/health-core/patients/$script:PatientId/timeline"
        Confirm-True (@(Find-ById $timeline.Body $script:ManualTimelineEventId).Count -eq 1) "manual timeline event appears in timeline"
    }

    Invoke-Section "Documents" {
        $document = Invoke-HealthCoreApi -Method POST -Path "/api/health-core/patients/$script:PatientId/documents" -ExpectedStatus 201 -Body @{
            documentType = "ClinicalNote"
            title = "Smoke document metadata"
            description = "Smoke document"
            documentDate = "2026-06-18T08:00:00Z"
            issuerName = "Smoke Clinic"
            fileName = "smoke.pdf"
            mimeType = "application/pdf"
            sourceType = "Manual"
            verificationStatus = "Unverified"
            sensitivityLevel = "Normal"
        }

        $script:DocumentId = $document.Body.id
        Confirm-True (-not [string]::IsNullOrWhiteSpace("$script:DocumentId")) "document create returned an id"
        Confirm-True ($document.Body.title -eq "Smoke document metadata") "document create returned expected title"

        $documents = Invoke-HealthCoreApi -Method GET -Path "/api/health-core/patients/$script:PatientId/documents"
        Confirm-True (@(Find-ById $documents.Body $script:DocumentId).Count -eq 1) "document appears in documents list"
    }

    Invoke-Section "Paraclinical results" {
        $result = Invoke-HealthCoreApi -Method POST -Path "/api/health-core/patients/$script:PatientId/paraclinical-results" -ExpectedStatus 201 -Body @{
            resultType = "Lab"
            title = "Smoke lab panel"
            description = "Smoke paraclinical result"
            performedAt = "2026-06-17T08:00:00Z"
            resultDate = "2026-06-18T08:00:00Z"
            providerName = "Smoke Lab"
            summary = "Normal except smoke LDL"
            interpretation = "Smoke interpretation"
            isAbnormal = $true
            requiresFollowUp = $true
            followUpNote = "Smoke follow up"
            sourceType = "Manual"
            verificationStatus = "Unverified"
            sensitivityLevel = "Normal"
            labItems = @(
                @{
                    testName = "LDL smoke"
                    value = "130"
                    numericValue = 130
                    unit = "mg/dL"
                    referenceRange = "< 100"
                    isAbnormal = $true
                    interpretation = "High"
                    displayOrder = 1
                }
            )
        }

        $script:ParaclinicalResultId = $result.Body.id
        Confirm-True (-not [string]::IsNullOrWhiteSpace("$script:ParaclinicalResultId")) "paraclinical create returned an id"
        Confirm-True (@(Get-Items $result.Body.labItems).Count -ge 1) "paraclinical create returned at least one lab item"

        $results = Invoke-HealthCoreApi -Method GET -Path "/api/health-core/patients/$script:PatientId/paraclinical-results"
        Confirm-True (@(Find-ById $results.Body $script:ParaclinicalResultId).Count -eq 1) "paraclinical result appears in list"
    }

    Invoke-Section "Care plan" {
        $carePlan = Invoke-HealthCoreApi -Method POST -Path "/api/health-core/patients/$script:PatientId/care-plan" -ExpectedStatus 201 -Body @{
            category = "FollowUp"
            itemType = "Visit"
            title = "Smoke care plan item"
            description = "Smoke care plan"
            reason = "Smoke verification"
            requestedBy = "Smoke tester"
            assignedTo = "Care team"
            plannedAt = "2026-06-20T08:00:00Z"
            dueAt = "2026-06-25T08:00:00Z"
            status = "Planned"
            priority = "Normal"
            sourceType = "Manual"
            verificationStatus = "Unverified"
            sensitivityLevel = "Normal"
        }

        $script:CarePlanItemId = $carePlan.Body.id
        Confirm-True (-not [string]::IsNullOrWhiteSpace("$script:CarePlanItemId")) "care plan create returned an id"

        $items = Invoke-HealthCoreApi -Method GET -Path "/api/health-core/patients/$script:PatientId/care-plan"
        Confirm-True (@(Find-ById $items.Body $script:CarePlanItemId).Count -eq 1) "care plan item appears in list"

        $updated = Invoke-HealthCoreApi -Method PUT -Path "/api/health-core/care-plan-items/$script:CarePlanItemId" -Body @{
            status = "Completed"
            resultSummary = "Smoke completed"
        }

        Confirm-True ($updated.Body.status -eq "Completed") "care plan item updates to Completed"
    }

    Invoke-Section "Reminders" {
        $reminder = Invoke-HealthCoreApi -Method POST -Path "/api/health-core/patients/$script:PatientId/reminders" -ExpectedStatus 201 -Body @{
            reminderType = "FollowUp"
            title = "Smoke reminder"
            description = "Smoke reminder"
            dueAt = "2026-06-26T08:00:00Z"
            status = "Pending"
            priority = "Normal"
            audience = "Internal"
            channel = "Panel"
            sourceType = "Manual"
            sensitivityLevel = "Normal"
        }

        $script:ReminderId = $reminder.Body.id
        Confirm-True (-not [string]::IsNullOrWhiteSpace("$script:ReminderId")) "reminder create returned an id"

        $reminders = Invoke-HealthCoreApi -Method GET -Path "/api/health-core/patients/$script:PatientId/reminders"
        Confirm-True (@(Find-ById $reminders.Body $script:ReminderId).Count -eq 1) "reminder appears in list"

        $updated = Invoke-HealthCoreApi -Method PUT -Path "/api/health-core/reminders/$script:ReminderId" -Body @{
            status = "Done"
        }

        Confirm-True ($updated.Body.status -eq "Done") "reminder updates to Done"
    }

    Invoke-Section "Measurements" {
        $measurement1 = Invoke-HealthCoreApi -Method POST -Path "/api/health-core/patients/$script:PatientId/measurements" -ExpectedStatus 201 -Body @{
            measurementType = "Weight"
            displayName = "Weight"
            value = 70.5
            unit = "kg"
            measuredAt = "2026-06-01T08:00:00Z"
            method = "Scale"
            sourceType = "Manual"
            verificationStatus = "Unverified"
            sensitivityLevel = "Normal"
        }

        $measurement2 = Invoke-HealthCoreApi -Method POST -Path "/api/health-core/patients/$script:PatientId/measurements" -ExpectedStatus 201 -Body @{
            measurementType = "Weight"
            displayName = "Weight"
            value = 71.2
            unit = "kg"
            measuredAt = "2026-06-15T08:00:00Z"
            method = "Scale"
            sourceType = "Manual"
            verificationStatus = "Unverified"
            sensitivityLevel = "Normal"
        }

        $script:MeasurementId1 = $measurement1.Body.id
        $script:MeasurementId2 = $measurement2.Body.id

        Confirm-True (-not [string]::IsNullOrWhiteSpace("$script:MeasurementId1")) "first measurement create returned an id"
        Confirm-True (-not [string]::IsNullOrWhiteSpace("$script:MeasurementId2")) "second measurement create returned an id"

        $measurements = Invoke-HealthCoreApi -Method GET -Path "/api/health-core/patients/$script:PatientId/measurements"
        Confirm-True (@(Find-ById $measurements.Body $script:MeasurementId1).Count -eq 1) "first measurement appears in list"
        Confirm-True (@(Find-ById $measurements.Body $script:MeasurementId2).Count -eq 1) "second measurement appears in list"

        $weightMeasurements = Invoke-HealthCoreApi -Method GET -Path "/api/health-core/patients/$script:PatientId/measurements?measurementType=Weight"
        Confirm-True (@((Get-Items $weightMeasurements.Body) | Where-Object { $_.measurementType -eq "Weight" }).Count -ge 2) "measurementType=Weight filter returns weight measurements"
    }

    Invoke-Section "Timeline auto-events" {
        $timeline = Invoke-HealthCoreApi -Method GET -Path "/api/health-core/patients/$script:PatientId/timeline"

        Confirm-True (Has-RelatedTimelineEvent $timeline.Body "PatientDocument" $script:DocumentId) "document timeline event auto-created"
        Confirm-True (Has-RelatedTimelineEvent $timeline.Body "PatientParaclinicalResult" $script:ParaclinicalResultId) "paraclinical timeline event auto-created"
        Confirm-True (Has-RelatedTimelineEvent $timeline.Body "CarePlanItem" $script:CarePlanItemId) "care plan timeline event auto-created"
        Confirm-True (Has-RelatedTimelineEvent $timeline.Body "PatientReminder" $script:ReminderId) "reminder timeline event auto-created"
        Confirm-True (Has-RelatedTimelineEvent $timeline.Body "PatientMeasurement" $script:MeasurementId1) "first measurement timeline event auto-created"
        Confirm-True (Has-RelatedTimelineEvent $timeline.Body "PatientMeasurement" $script:MeasurementId2) "second measurement timeline event auto-created"
    }

    Write-Host ""
    Write-Host "Smoke test completed successfully." -ForegroundColor Green
    Write-Host "Created test patient id: $script:PatientId"
    Write-Host "Note: this first version leaves the created smoke-test records in the local database."

    exit 0
} catch {
    Write-Host ""
    Write-Host "Smoke test failed." -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red

    exit 1
} finally {
    $httpClient.Dispose()
}
