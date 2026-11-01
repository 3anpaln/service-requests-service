# Shared HTTP helper for Windows PowerShell 5.1 and PowerShell 7.
Add-Type -AssemblyName System.Net.Http

function Invoke-ServiceRequestApi {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$BaseUrl,

        [Parameter(Mandatory = $true)]
        [ValidateSet('GET', 'POST', 'PATCH')]
        [string]$Method,

        [Parameter(Mandatory = $true)]
        [string]$Path,

        [object]$Body,
        [int]$ExpectedStatus = 200
    )

    $uri = $BaseUrl.TrimEnd('/') + '/' + $Path.TrimStart('/')
    $client = [System.Net.Http.HttpClient]::new()
    $client.Timeout = [TimeSpan]::FromSeconds(30)
    $request = [System.Net.Http.HttpRequestMessage]::new(
        [System.Net.Http.HttpMethod]::new($Method), $uri)
    $response = $null

    try {
        if ($PSBoundParameters.ContainsKey('Body')) {
            $json = ConvertTo-Json -InputObject $Body -Depth 10 -Compress
            $request.Content = [System.Net.Http.StringContent]::new(
                $json, [System.Text.Encoding]::UTF8, 'application/json')
        }

        $response = $client.SendAsync($request).GetAwaiter().GetResult()
        $statusCode = [int]$response.StatusCode
        $rawBody = ''
        if ($null -ne $response.Content) {
            $rawBody = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        }
        Write-Host "$Method $uri -> HTTP $statusCode (expected $ExpectedStatus)"

        if ($statusCode -ne $ExpectedStatus) {
            throw "Expected HTTP $ExpectedStatus, received HTTP $statusCode. Response: $rawBody"
        }

        $parsedBody = $null
        if (-not [string]::IsNullOrWhiteSpace($rawBody)) {
            $parsedBody = ConvertFrom-Json -InputObject $rawBody -ErrorAction Stop
        }

        return [pscustomobject]@{
            StatusCode = $statusCode
            Body = $parsedBody
            RawBody = $rawBody
        }
    }
    catch {
        throw "${Method} ${uri}: $($_.Exception.Message)"
    }
    finally {
        if ($null -ne $response) { $response.Dispose() }
        $request.Dispose()
        $client.Dispose()
    }
}
