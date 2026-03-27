param(
    [string]$FtpRootPath = "C:\inetpub\ftproot",
    [string]$SourceFolderName = "MeterReading",
    [string]$TargetFolderName = "BankConsents"
)

Write-Host "=== Enable /BankConsents FTP Path ===" -ForegroundColor Cyan
Write-Host "This script must be run on the FTP server as Administrator." -ForegroundColor Yellow
Write-Host ""

$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
$isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "ERROR: Run PowerShell as Administrator." -ForegroundColor Red
    exit 1
}

$sourcePath = Join-Path $FtpRootPath $SourceFolderName
$targetPath = Join-Path $FtpRootPath $TargetFolderName

Write-Host "FTP root      : $FtpRootPath" -ForegroundColor White
Write-Host "Source folder : $sourcePath" -ForegroundColor White
Write-Host "Target folder : $targetPath" -ForegroundColor White
Write-Host ""

if (-not (Test-Path $sourcePath)) {
    Write-Host "ERROR: Source folder does not exist: $sourcePath" -ForegroundColor Red
    Write-Host "MeterReading must exist first because its permissions are used as the template." -ForegroundColor Yellow
    exit 1
}

if (-not (Test-Path $targetPath)) {
    New-Item -Path $targetPath -ItemType Directory -Force | Out-Null
    Write-Host "Created target folder." -ForegroundColor Green
} else {
    Write-Host "Target folder already exists." -ForegroundColor Green
}

Write-Host "Copying NTFS ACL from MeterReading to BankConsents..." -ForegroundColor Cyan
$sourceAcl = Get-Acl $sourcePath
Set-Acl -Path $targetPath -AclObject $sourceAcl
Write-Host "NTFS ACL copied successfully." -ForegroundColor Green

Write-Host ""
Write-Host "Testing local write/delete in BankConsents..." -ForegroundColor Cyan
$testFile = Join-Path $targetPath ("perm_test_" + [guid]::NewGuid().ToString("N") + ".txt")
"permission test" | Out-File -FilePath $testFile -Encoding ascii
Remove-Item $testFile -Force
Write-Host "Local write/delete test passed." -ForegroundColor Green

Write-Host ""
Write-Host "Current ACL on BankConsents:" -ForegroundColor Cyan
(Get-Acl $targetPath).Access |
    Select-Object IdentityReference, FileSystemRights, AccessControlType, IsInherited |
    Format-Table -AutoSize

Write-Host ""
Write-Host "If your FTP site root is C:\inetpub\ftproot, /BankConsents should now map directly to:" -ForegroundColor Yellow
Write-Host "  $targetPath" -ForegroundColor White
Write-Host ""
Write-Host "Next tests:" -ForegroundColor Yellow
Write-Host "1. On the API machine, test raw FTP upload to ftp://192.168.40.47/BankConsents" -ForegroundColor White
Write-Host "2. If that succeeds, set Web.config Ftp_BasePath back to /BankConsents" -ForegroundColor White
Write-Host "3. Rebuild and re-test the consent upload/download API" -ForegroundColor White