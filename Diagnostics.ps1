# Diagnostic Script for Consent Document Download
# Run this PowerShell script to check your setup

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Consent Document Download Diagnostics" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Test parameters
$requirementId = "REQ0000000024"
$bankId = "BNK00006"
$fileName = "ApplicationDetails_20260104171559.pdf"
$networkPath = "\\192.168.40.47\c$\inetpub\ftproot\BankConsents"
$localPath = "C:\inetpub\ftproot\BankConsents"
$logPath = "C:\smkcapi_published\Logs\FtpLog_$(Get-Date -Format 'yyyyMMdd').txt"

Write-Host "Test Parameters:" -ForegroundColor Yellow
Write-Host "  Requirement ID: $requirementId"
Write-Host "  Bank ID: $bankId"
Write-Host "  File Name: $fileName"
Write-Host ""

# Check 1: Log file exists
Write-Host "Check 1: Log File" -ForegroundColor Yellow
if (Test-Path $logPath) {
    Write-Host "  ? Log file exists: $logPath" -ForegroundColor Green
    $logSize = (Get-Item $logPath).Length / 1KB
    Write-Host "  ? Log size: $([math]::Round($logSize, 2)) KB" -ForegroundColor Green
} else {
    Write-Host "  ? Log file not found: $logPath" -ForegroundColor Red
}
Write-Host ""

# Check 2: Network path accessible
Write-Host "Check 2: Network Path Access" -ForegroundColor Yellow
try {
    if (Test-Path $networkPath -ErrorAction Stop) {
        Write-Host "  ? Network path accessible: $networkPath" -ForegroundColor Green
    } else {
        Write-Host "  ? Network path not accessible: $networkPath" -ForegroundColor Red
    }
} catch {
    Write-Host "  ? Cannot access network path: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Check 3: Local path (if on same server)
Write-Host "Check 3: Local Path (if API on same server)" -ForegroundColor Yellow
if (Test-Path $localPath) {
    Write-Host "  ? Local path exists: $localPath" -ForegroundColor Green
} else {
    Write-Host "  ? Local path not found (OK if API is on different server)" -ForegroundColor Gray
}
Write-Host ""

# Check 4: Requirement directory
Write-Host "Check 4: Requirement Directory" -ForegroundColor Yellow
$reqDirNetwork = Join-Path $networkPath $requirementId
$reqDirLocal = Join-Path $localPath $requirementId

try {
    if (Test-Path $reqDirNetwork -ErrorAction Stop) {
        Write-Host "  ? Requirement directory exists: $reqDirNetwork" -ForegroundColor Green
    } else {
        Write-Host "  ? Requirement directory not found: $reqDirNetwork" -ForegroundColor Red
    }
} catch {
    Write-Host "  ? Cannot check requirement directory: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Check 5: Bank directory
Write-Host "Check 5: Bank Directory" -ForegroundColor Yellow
$bankDirNetwork = Join-Path (Join-Path $networkPath $requirementId) $bankId
$bankDirLocal = Join-Path (Join-Path $localPath $requirementId) $bankId

try {
    if (Test-Path $bankDirNetwork -ErrorAction Stop) {
        Write-Host "  ? Bank directory exists: $bankDirNetwork" -ForegroundColor Green
        
        # List files in bank directory
        $files = Get-ChildItem $bankDirNetwork -File -ErrorAction SilentlyContinue
        if ($files.Count -gt 0) {
            Write-Host "  ? Files found ($($files.Count)):" -ForegroundColor Green
            foreach ($file in $files) {
                Write-Host "    - $($file.Name) ($([math]::Round($file.Length / 1KB, 2)) KB)" -ForegroundColor White
            }
        } else {
            Write-Host "  ? Directory is empty (no files)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "  ? Bank directory not found: $bankDirNetwork" -ForegroundColor Red
    }
} catch {
    Write-Host "  ? Cannot check bank directory: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Check 6: Specific file
Write-Host "Check 6: Target File" -ForegroundColor Yellow
$filePathNetwork = Join-Path (Join-Path (Join-Path $networkPath $requirementId) $bankId) $fileName
$filePathLocal = Join-Path (Join-Path (Join-Path $localPath $requirementId) $bankId) $fileName

try {
    if (Test-Path $filePathNetwork -ErrorAction Stop) {
        Write-Host "  ? File EXISTS: $filePathNetwork" -ForegroundColor Green
        $fileInfo = Get-Item $filePathNetwork
        Write-Host "  ? File size: $([math]::Round($fileInfo.Length / 1KB, 2)) KB" -ForegroundColor Green
        Write-Host "  ? Created: $($fileInfo.CreationTime)" -ForegroundColor Green
        Write-Host "  ? Modified: $($fileInfo.LastWriteTime)" -ForegroundColor Green
    } else {
        Write-Host "  ? File NOT FOUND: $filePathNetwork" -ForegroundColor Red
        Write-Host "  ? This is why you're getting 404 error" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  ? Cannot check file: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Check 7: Recent log entries
Write-Host "Check 7: Recent Log Entries" -ForegroundColor Yellow
if (Test-Path $logPath) {
    Write-Host "  Last 20 log entries:" -ForegroundColor White
    Get-Content $logPath -Tail 20 | ForEach-Object {
        if ($_ -match "ERROR") {
            Write-Host "  $_" -ForegroundColor Red
        } elseif ($_ -match "WARN") {
            Write-Host "  $_" -ForegroundColor Yellow
        } else {
            Write-Host "  $_" -ForegroundColor Gray
        }
    }
} else {
    Write-Host "  ? Log file not available" -ForegroundColor Red
}
Write-Host ""

# Check 8: Network connectivity
Write-Host "Check 8: Network Connectivity" -ForegroundColor Yellow
try {
    $ping = Test-Connection -ComputerName "192.168.40.47" -Count 2 -Quiet
    if ($ping) {
        Write-Host "  ? Server 192.168.40.47 is reachable" -ForegroundColor Green
    } else {
        Write-Host "  ? Server 192.168.40.47 is not reachable" -ForegroundColor Red
    }
} catch {
    Write-Host "  ? Cannot ping server: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

if (Test-Path $filePathNetwork -ErrorAction SilentlyContinue) {
    Write-Host "? File exists - Download should work!" -ForegroundColor Green
    Write-Host "Test with: GET /api/deposits/consent/download?requirementId=$requirementId&bankId=$bankId&fileName=$fileName" -ForegroundColor White
} else {
    Write-Host "? File not found - 404 error is expected" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Next Steps:" -ForegroundColor Yellow
    Write-Host "1. Check if file was uploaded via quote submission API" -ForegroundColor White
    Write-Host "2. Verify file name matches exactly (case-sensitive)" -ForegroundColor White
    Write-Host "3. Create test file manually at:" -ForegroundColor White
    Write-Host "   $filePathNetwork" -ForegroundColor Gray
    Write-Host "4. Check detailed logs for more information:" -ForegroundColor White
    Write-Host "   $logPath" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Diagnostic complete!" -ForegroundColor Cyan
Write-Host ""

# Offer to open log
$openLog = Read-Host "Open log file? (Y/N)"
if ($openLog -eq "Y" -or $openLog -eq "y") {
    if (Test-Path $logPath) {
        notepad $logPath
    } else {
        Write-Host "Log file not found" -ForegroundColor Red
    }
}
