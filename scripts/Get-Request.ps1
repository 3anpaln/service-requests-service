#requires -Version 5.1
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [Guid]$Id,
    [string]$BaseUrl = 'http://localhost:8080'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Api.Common.ps1')

# Exactly one API call for the supplied request ID.
$result = Invoke-ServiceRequestApi -BaseUrl $BaseUrl -Method GET `
    -Path "/api/service-requests/$Id" -ExpectedStatus 200
Write-Output $result.RawBody
