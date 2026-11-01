#requires -Version 5.1
[CmdletBinding()]
param([string]$BaseUrl = 'http://localhost:8080')

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Api.Common.ps1')

# Exactly one API call; print the collection JSON returned by the application.
$result = Invoke-ServiceRequestApi -BaseUrl $BaseUrl -Method GET `
    -Path '/api/service-requests' -ExpectedStatus 200
Write-Output $result.RawBody
