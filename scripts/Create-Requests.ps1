#requires -Version 5.1
[CmdletBinding()]
param(
    [ValidateRange(1, 20)]
    [int]$Count = 20,
    [string]$BaseUrl = 'http://localhost:8080'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Api.Common.ps1')

$batch = [Guid]::NewGuid().ToString('N').Substring(0, 8)
for ($number = 1; $number -le $Count; $number++) {
    $body = @{
        title = "Demo request $number ($batch)"
        description = "Sample service request created by Create-Requests.ps1."
        priority = (($number - 1) % 3) + 1
    }
    $result = Invoke-ServiceRequestApi -BaseUrl $BaseUrl -Method POST `
        -Path '/api/service-requests' -Body $body -ExpectedStatus 201
    if (-not $result.Body.id) { throw 'The created resource did not contain an id.' }
    Write-Output $result.Body
}
