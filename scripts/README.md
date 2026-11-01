# API demo scripts

Requires Windows PowerShell 5.1 or PowerShell 7 and the running API. No Git, extra PowerShell modules, or test framework is needed. Run from the repository root (`~service-requests-service`).

```powershell
# Create 1-20 requests (20 by default); capture the created resources and their IDs.
$requests = @(.\scripts\Create-Requests.ps1 -Count 20)
$requests | Format-Table id, title, priority, status

# One GET call to retrieve the current collection.
.\scripts\Get-Requests.ps1

# One GET call to retrieve a resource by its ID.
.\scripts\Get-Request.ps1 -Id $requests[0].id

# Create one new request and verify New -> InProgress -> Done (201/204/200).
.\scripts\Test-HappyPath.ps1

# Create one new request and check five forbidden transitions (409).
.\scripts\Test-InvalidTransitions.ps1

# Check six invalid creation payloads (400); print the Problem Details bodies.
.\scripts\Test-InvalidRequests.ps1
```

Every script accepts `-BaseUrl`, defaulting to `http://localhost:8080`:

```powershell
.\scripts\Get-Requests.ps1 -BaseUrl 'http://localhost:5000'
```

The transition scripts create their own requests, so they can be rerun without an existing ID. The creation limit applies to each `Create-Requests.ps1` invocation; the two transition scripts each create one additional request. Demo data remains in the application. The scripts do not delete or reset existing requests.

Each HTTP call prints its actual and expected status. Unexpected responses or connection failures stop the script with an error; the negative checks only pass for the expected rejection. PATCH bodies are bare numeric statuses: New=1, InProgress=2, Done=3, Cancelled=4. Priorities are Low=1, Normal=2, High=3.

If local execution policy blocks scripts, allow them only for the current terminal session:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
```

`Api.Common.ps1` is a shared helper; run the other six scripts directly.
