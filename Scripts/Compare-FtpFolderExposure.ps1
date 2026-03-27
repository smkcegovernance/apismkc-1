param(
    [string]$FtpSiteName = "fileserver",
    [string]$FtpRootPath = "C:\inetpub\ftproot",
    [string]$FolderA = "MeterReading",
    [string]$FolderB = "BankConsents"
)

Write-Host "=== Compare FTP Folder Exposure ===" -ForegroundColor Cyan
Write-Host "Run on the FTP server as Administrator." -ForegroundColor Yellow
Write-Host ""

$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
$isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: Run PowerShell as Administrator." -ForegroundColor Red
    exit 1
}

Import-Module WebAdministration -ErrorAction Stop

$pathA = Join-Path $FtpRootPath $FolderA
$pathB = Join-Path $FtpRootPath $FolderB

Write-Host "FTP site   : $FtpSiteName" -ForegroundColor White
Write-Host "Folder A   : $pathA" -ForegroundColor White
Write-Host "Folder B   : $pathB" -ForegroundColor White
Write-Host ""

foreach ($path in @($pathA, $pathB)) {
    Write-Host "Checking path: $path" -ForegroundColor Cyan
    Write-Host "  Exists: $((Test-Path $path))" -ForegroundColor White
    if (Test-Path $path) {
        (Get-Acl $path).Access |
            Select-Object IdentityReference, FileSystemRights, AccessControlType, IsInherited |
            Format-Table -AutoSize
    }
    Write-Host ""
}

$site = Get-ChildItem IIS:\Sites | Where-Object { $_.Name -eq $FtpSiteName }
if (-not $site) {
    Write-Host "ERROR: FTP site not found: $FtpSiteName" -ForegroundColor Red
    exit 1
}

Write-Host "Site state: $($site.state)" -ForegroundColor White
Write-Host "Bindings:" -ForegroundColor Cyan
foreach ($binding in $site.bindings.Collection) {
    if ($binding.protocol -eq 'ftp') {
        Write-Host "  $($binding.bindingInformation)" -ForegroundColor White
    }
}
Write-Host ""

Write-Host "Virtual directories under site/application:" -ForegroundColor Cyan
try {
    Get-WebVirtualDirectory -Site $FtpSiteName |
        Select-Object Path, PhysicalPath |
        Format-Table -AutoSize
} catch {
    Write-Host "  Could not enumerate virtual directories: $($_.Exception.Message)" -ForegroundColor Yellow
}
Write-Host ""

Write-Host "FTP authorization rules:" -ForegroundColor Cyan
try {
    $authPath = "IIS:\Sites\$FtpSiteName"
    Get-WebConfiguration -PSPath $authPath -Filter "system.ftpServer/security/authorization" |
        ForEach-Object { $_.Collection } |
        ForEach-Object {
            $_ | Select-Object accessType, roles, users, permissions
        } | Format-Table -AutoSize
} catch {
    Write-Host "  Could not read authorization rules: $($_.Exception.Message)" -ForegroundColor Yellow
}
Write-Host ""

Write-Host "FTP user isolation:" -ForegroundColor Cyan
try {
    $isolation = Get-WebConfigurationProperty -PSPath 'MACHINE/WEBROOT/APPHOST' -Filter "system.ftpServer/userIsolation" -Name "."
    $isolation | Format-List | Out-String | Write-Host
} catch {
    Write-Host "  Could not read user isolation settings: $($_.Exception.Message)" -ForegroundColor Yellow
}
Write-Host ""

Write-Host "Interpretation:" -ForegroundColor Yellow
Write-Host "- If MeterReading works but BankConsents does not, and both folders exist with same ACL, then IIS FTP mapping or user isolation is the likely difference." -ForegroundColor White
Write-Host "- If BankConsents is not inside the same effective FTP site root, create it as a virtual directory under the FTP site." -ForegroundColor White
Write-Host "- If user isolation is enabled, ensure BankConsents is reachable in that isolated user path." -ForegroundColor White