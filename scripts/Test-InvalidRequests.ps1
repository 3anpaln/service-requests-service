#requires -Version 5.1
[CmdletBinding()]
param([string]$BaseUrl = 'http://localhost:8080')

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Api.Common.ps1')

$cases = @(
    @{ Name = 'Missing title'; Body = @{ priority = 2 } }
    @{ Name = 'Empty title'; Body = @{ title = ''; priority = 2 } }
    @{ Name = 'Whitespace title'; Body = @{ title = '   '; priority = 2 } }
    @{ Name = 'Title longer than 200 characters'; Body = @{ title = ('x' * 201); priority = 2 } }
    @{ Name = 'Description longer than 2000 characters'; Body = @{ title = 'Invalid description'; description = ('x' * 2001); priority = 2 } }
    @{ Name = 'Undefined priority'; Body = @{ title = 'Invalid priority'; priority = 999 } }
)

foreach ($case in $cases) {
    Write-Host $case.Name
    $result = Invoke-ServiceRequestApi -BaseUrl $BaseUrl -Method POST `
        -Path '/api/service-requests' -Body $case.Body -ExpectedStatus 400
    if ([int]$result.Body.status -ne 400) {
        throw "Expected a Problem Details response with status 400 for '$($case.Name)'."
    }
    Write-Output $result.RawBody
}

Write-Host 'PASS: all six invalid request bodies returned HTTP 400.'
