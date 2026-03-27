# Run this script on 192.168.40.47 via Remote Desktop
# Right-click PowerShell -> Run as Administrator
# ?? WARNING: This weakens UAC security - use only for testing!

Write-Host "Enabling remote admin share access..." -ForegroundColor Yellow
Write-Host "?? WARNING: This weakens UAC security!" -ForegroundColor Red
Write-Host ""

$confirm = Read-Host "Continue? (type YES to proceed)"
if ($confirm -ne "YES") {
    Write-Host "Cancelled." -ForegroundColor Gray
    exit
}

# Enable LocalAccountTokenFilterPolicy
$regPath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"
Set-ItemProperty -Path $regPath -Name "LocalAccountTokenFilterPolicy" -Value 1 -Type DWord

Write-Host "? Registry key set" -ForegroundColor Green
Write-Host ""
Write-Host "?? SERVER RESTART REQUIRED!" -ForegroundColor Red
Write-Host ""

$restart = Read-Host "Restart server now? (Y/N)"
if ($restart -eq "Y" -or $restart -eq "y") {
    Write-Host "Restarting in 10 seconds..." -ForegroundColor Yellow
    Start-Sleep -Seconds 10
    Restart-Computer -Force
} else {
    Write-Host "Please restart manually for changes to take effect" -ForegroundColor Yellow
}
