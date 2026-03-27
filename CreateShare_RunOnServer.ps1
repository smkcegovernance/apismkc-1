# Run this script on 192.168.40.47 via Remote Desktop
# Right-click PowerShell -> Run as Administrator

Write-Host "Creating regular share for BankConsents..." -ForegroundColor Cyan

# Create the share
New-SmbShare -Name "BankConsents" `
             -Path "C:\inetpub\ftproot\BankConsents" `
             -FullAccess "Everyone" `
             -ErrorAction SilentlyContinue

Write-Host "? Share created: \\192.168.40.47\BankConsents" -ForegroundColor Green

# Grant NTFS permissions
$acl = Get-Acl "C:\inetpub\ftproot\BankConsents"
$permission = "Everyone","Modify","ContainerInherit,ObjectInherit","None","Allow"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
$acl.SetAccessRule($accessRule)
Set-Acl "C:\inetpub\ftproot\BankConsents" $acl

Write-Host "? Permissions granted" -ForegroundColor Green

# Verify
Get-SmbShare -Name "BankConsents"

Write-Host ""
Write-Host "Test access from your dev machine with:" -ForegroundColor Yellow
Write-Host "  Test-Path '\\192.168.40.47\BankConsents'" -ForegroundColor White
Write-Host ""
Write-Host "Then update Web.config:" -ForegroundColor Yellow
Write-Host '  <add key="Network_Share" value="BankConsents" />' -ForegroundColor White
