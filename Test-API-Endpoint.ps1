# Quick Test Script for ConsentDocumentController
# Tests if the API endpoint is accessible

param(
    [string]$BaseUrl = "http://localhost:57031"
)

Write-Host "=== Testing ConsentDocumentController ===" -ForegroundColor Cyan
Write-Host ""

$healthUrl = "$BaseUrl/api/deposits/consent/health"

Write-Host "Testing: $healthUrl" -ForegroundColor Yellow
Write-Host ""

try {
    $response = Invoke-RestMethod -Uri $healthUrl -Method Get -ErrorAction Stop
    
    Write-Host "? SUCCESS - Endpoint is accessible!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Response:" -ForegroundColor White
    $response | ConvertTo-Json -Depth 3
    Write-Host ""
    
    if ($response.success -eq $true) {
        Write-Host "? Health check passed" -ForegroundColor Green
        Write-Host "? Controller is working correctly" -ForegroundColor Green
        Write-Host "? If you set a breakpoint, it should hit now" -ForegroundColor Green
    }
}
catch {
    Write-Host "? FAILED - Endpoint not accessible" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error Details:" -ForegroundColor Yellow
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    
    if ($_.Exception.Message -like "*Unable to connect*" -or $_.Exception.Message -like "*No connection*") {
        Write-Host "Possible Causes:" -ForegroundColor Yellow
        Write-Host "1. API is not running (Press F5 in Visual Studio)" -ForegroundColor White
        Write-Host "2. Wrong port number (check project properties)" -ForegroundColor White
        Write-Host "3. IIS Express not started" -ForegroundColor White
    }
    elseif ($_.Exception.Response.StatusCode -eq 401) {
        Write-Host "Note: This is an authentication test endpoint" -ForegroundColor Yellow
        Write-Host "It should NOT require authentication" -ForegroundColor White
        Write-Host "Check [AllowAnonymous] attribute is present" -ForegroundColor White
    }
    else {
        Write-Host "Status Code: $($_.Exception.Response.StatusCode)" -ForegroundColor White
        Write-Host "Check Visual Studio Output window for errors" -ForegroundColor White
    }
}

Write-Host ""
Write-Host "=== Quick Debug Checklist ===" -ForegroundColor Cyan
Write-Host "? Visual Studio is open" -ForegroundColor White
Write-Host "? Solution is loaded" -ForegroundColor White
Write-Host "? Configuration is set to 'Debug'" -ForegroundColor White
Write-Host "? Pressed F5 (not Ctrl+F5)" -ForegroundColor White
Write-Host "? IIS Express icon in system tray" -ForegroundColor White
Write-Host "? Breakpoint set in HealthCheck() method" -ForegroundColor White
Write-Host "? Breakpoint is solid red circle (not hollow)" -ForegroundColor White
Write-Host ""
