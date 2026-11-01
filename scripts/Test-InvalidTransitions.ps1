#requires -Version 5.1
[CmdletBinding()]
param([string]$BaseUrl = 'http://localhost:8080')

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Api.Common.ps1')

$created = Invoke-ServiceRequestApi -BaseUrl $BaseUrl -Method POST `
    -Path '/api/service-requests' -Body @{ title = 'Invalid transitions demo'; priority = 2 } `
    -ExpectedStatus 201
[Guid]$requestId = $created.Body.id
if ($requestId -eq [Guid]::Empty -or [int]$created.Body.status -ne 1) {
    throw 'Creation must return a resource ID and status New (1).'
}

# Valid steps move our own demo request between the states needed for each check.
$steps = @(
    @{ Name = 'New -> Done is forbidden'; Target = 3; Expected = 409; Stored = 1 }
    @{ Name = 'New -> New is forbidden'; Target = 1; Expected = 409; Stored = 1 }
    @{ Name = 'New -> Cancelled'; Target = 4; Expected = 204; Stored = 4 }
    @{ Name = 'Cancelled -> InProgress is forbidden'; Target = 2; Expected = 409; Stored = 4 }
    @{ Name = 'Cancelled -> New'; Target = 1; Expected = 204; Stored = 1 }
    @{ Name = 'New -> InProgress'; Target = 2; Expected = 204; Stored = 2 }
    @{ Name = 'InProgress -> New is forbidden'; Target = 1; Expected = 409; Stored = 2 }
    @{ Name = 'InProgress -> Done'; Target = 3; Expected = 204; Stored = 3 }
    @{ Name = 'Done -> New is forbidden'; Target = 1; Expected = 409; Stored = 3 }
)

foreach ($step in $steps) {
    Write-Host $step.Name
    $result = Invoke-ServiceRequestApi -BaseUrl $BaseUrl -Method PATCH `
        -Path "/api/service-requests/$requestId/status" -Body $step.Target `
        -ExpectedStatus $step.Expected
    if ($step.Expected -eq 409) { Write-Output $result.RawBody }

    $stored = Invoke-ServiceRequestApi -BaseUrl $BaseUrl -Method GET `
        -Path "/api/service-requests/$requestId" -ExpectedStatus 200
    if ([int]$stored.Body.status -ne $step.Stored) {
        throw "Unexpected persisted status after '$($step.Name)'."
    }
}

Write-Host 'PASS: five forbidden transitions returned HTTP 409 and preserved the current status.'
