#requires -Version 5.1
[CmdletBinding()]
param([string]$BaseUrl = 'http://localhost:8080')

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Api.Common.ps1')

$created = Invoke-ServiceRequestApi -BaseUrl $BaseUrl -Method POST `
    -Path '/api/service-requests' -Body @{ title = 'Happy path demo'; priority = 3 } `
    -ExpectedStatus 201
[Guid]$requestId = $created.Body.id
if ($requestId -eq [Guid]::Empty -or [int]$created.Body.status -ne 1) {
    throw 'Creation must return a resource ID and status New (1).'
}

# New (1) -> InProgress (2) -> Done (3).
foreach ($status in @(2, 3)) {
    $null = Invoke-ServiceRequestApi -BaseUrl $BaseUrl -Method PATCH `
        -Path "/api/service-requests/$requestId/status" -Body $status -ExpectedStatus 204
    $stored = Invoke-ServiceRequestApi -BaseUrl $BaseUrl -Method GET `
        -Path "/api/service-requests/$requestId" -ExpectedStatus 200
    if ([Guid]$stored.Body.id -ne $requestId -or [int]$stored.Body.status -ne $status) {
        throw "Expected request $requestId to have status $status after the update."
    }
}

Write-Host "PASS: request $requestId completed New -> InProgress -> Done."
Write-Output $stored.Body
