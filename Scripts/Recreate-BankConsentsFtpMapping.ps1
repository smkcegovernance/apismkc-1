param(
    [string]$FtpSiteName = "fileserver",
    [string]$Alias = "BankConsents",
    [string]$PhysicalPath,
    [string]$TestUser = "administrator",
    [string]$TestPassword = "smkc@1234",
    [string]$UserIsolationMode = "None",
    [int]$FtpTimeoutMs = 15000,
    [switch]$RepairAcl,
    [switch]$ResetPhysicalFolder,
    [switch]$TestWrite
)

if (-not $PSBoundParameters.ContainsKey('RepairAcl')) {
    $RepairAcl = $true
}

function Get-FtpSiteRootPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SiteName
    )

    $site = Get-ChildItem IIS:\Sites | Where-Object { $_.Name -eq $SiteName }
    if (-not $site) {
        throw "FTP site not found: $SiteName"
    }

    $filter = "system.applicationHost/sites/site[@name='$SiteName']/application[@path='/']/virtualDirectory[@path='/']"
    $rootPath = (Get-WebConfigurationProperty -PSPath 'MACHINE/WEBROOT/APPHOST' -Filter $filter -Name physicalPath -ErrorAction Stop).Value
    if ([string]::IsNullOrWhiteSpace($rootPath)) {
        throw "Root virtual directory physicalPath not found for FTP site: $SiteName"
    }

    return [Environment]::ExpandEnvironmentVariables($rootPath)
}

function Remove-FtpVirtualDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SiteName,
        [Parameter(Mandatory = $true)]
        [string]$VirtualPath
    )

    $aliasName = $VirtualPath.Trim('/')
    $existing = Get-WebVirtualDirectory -Site $SiteName -Application "/" -Name $aliasName -ErrorAction SilentlyContinue
    if ($existing) {
        Remove-WebVirtualDirectory -Site $SiteName -Application "/" -Name $aliasName -ErrorAction Stop
        return $true
    }

    return $false
}

function Repair-FolderAcl {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FolderPath,
        [Parameter(Mandatory = $true)]
        [string]$UserName
    )

    if (-not (Test-Path $FolderPath)) {
        throw "Folder not found for ACL repair: $FolderPath"
    }

    & icacls $FolderPath /inheritance:e | Out-Null
    & icacls $FolderPath /grant "${UserName}:(OI)(CI)F" | Out-Null
    & icacls $FolderPath /grant "Administrators:(OI)(CI)F" | Out-Null
    & icacls $FolderPath /grant "Users:(OI)(CI)M" | Out-Null
    Write-Host "Applied explicit ACL grants on $FolderPath" -ForegroundColor Green
}

function Clear-FtpPathOverride {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AppCmd,
        [Parameter(Mandatory = $true)]
        [string]$LocationPath
    )

    $sections = @(
        'system.ftpServer/security/authorization',
        'system.ftpServer/security/requestFiltering'
    )

    foreach ($section in $sections) {
        try {
            & $AppCmd clear config "$LocationPath" /section:$section /commit:apphost | Out-Null
            Write-Host "Cleared FTP override: $LocationPath [$section]" -ForegroundColor Green
        } catch {
            Write-Host "No override (or cannot clear): $LocationPath [$section]" -ForegroundColor Gray
        }
    }
}

Write-Host "=== Recreate BankConsents FTP Mapping ===" -ForegroundColor Cyan
Write-Host "Run this on the FTP server as Administrator." -ForegroundColor Yellow
Write-Host ""

$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
$isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: Run PowerShell as Administrator." -ForegroundColor Red
    exit 1
}

Import-Module WebAdministration -ErrorAction Stop

$appcmd = Join-Path $env:windir "System32\inetsrv\appcmd.exe"
if (-not (Test-Path $appcmd)) {
    Write-Host "ERROR: appcmd.exe not found at $appcmd" -ForegroundColor Red
    exit 1
}

try {
    $siteRootPath = Get-FtpSiteRootPath -SiteName $FtpSiteName
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

if ([string]::IsNullOrWhiteSpace($PhysicalPath)) {
    $PhysicalPath = Join-Path $siteRootPath $Alias
}

Write-Host "FTP site      : $FtpSiteName" -ForegroundColor White
Write-Host "Site root     : $siteRootPath" -ForegroundColor White
Write-Host "Alias         : $Alias" -ForegroundColor White
Write-Host "Physical path : $PhysicalPath" -ForegroundColor White
Write-Host "Test user     : $TestUser" -ForegroundColor White
Write-Host "Isolation mode: $UserIsolationMode" -ForegroundColor White
Write-Host "FTP timeout   : $FtpTimeoutMs ms" -ForegroundColor White
Write-Host "Repair ACL    : $RepairAcl" -ForegroundColor White
Write-Host ""

Write-Host ""
Write-Host "Step 1: Stop FTP site..." -ForegroundColor Cyan
try {
    & $appcmd stop site /site.name:"$FtpSiteName" | Out-Null
    Write-Host "FTP site stopped." -ForegroundColor Green
} catch {
    Write-Host "Warning: could not stop FTP site cleanly: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Step 2: Remove stale IIS virtual directory if present..." -ForegroundColor Cyan
try {
    if (Remove-FtpVirtualDirectory -SiteName $FtpSiteName -VirtualPath "/$Alias") {
        Write-Host "Removed stale virtual directory /$Alias" -ForegroundColor Green
    } else {
        Write-Host "No virtual directory found for /$Alias" -ForegroundColor Gray
    }
} catch {
    Write-Host "Warning: could not remove virtual directory /$Alias : $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Step 3: Recreate physical folder under the effective FTP root..." -ForegroundColor Cyan
if ($ResetPhysicalFolder -and (Test-Path $PhysicalPath)) {
    Remove-Item -Path $PhysicalPath -Recurse -Force
    Write-Host "Removed existing folder: $PhysicalPath" -ForegroundColor Green
}

if (-not (Test-Path $PhysicalPath)) {
    New-Item -Path $PhysicalPath -ItemType Directory -Force | Out-Null
    Write-Host "Created physical folder: $PhysicalPath" -ForegroundColor Green
} else {
    Write-Host "Physical folder already exists." -ForegroundColor Green
}

if ($RepairAcl) {
    try {
        Repair-FolderAcl -FolderPath $PhysicalPath -UserName $TestUser
    } catch {
        Write-Host "Warning: ACL repair failed: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "Step 4: Set FTP user isolation mode..." -ForegroundColor Cyan
try {
    & $appcmd set config /section:system.applicationHost/sites "/[name='$FtpSiteName'].ftpServer.userIsolation.mode:$UserIsolationMode" /commit:apphost | Out-Null
    Write-Host "Set FTP user isolation to '$UserIsolationMode' for site '$FtpSiteName'" -ForegroundColor Green
} catch {
    Write-Host "Warning: failed to set FTP user isolation mode: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Step 5: Recreate explicit FTP authorization rule..." -ForegroundColor Cyan
try {
    & $appcmd set config $FtpSiteName /section:system.ftpServer/security/authentication/anonymousAuthentication /enabled:false /commit:apphost | Out-Null
    & $appcmd set config $FtpSiteName /section:system.ftpServer/security/authentication/basicAuthentication /enabled:true /commit:apphost | Out-Null

    & $appcmd set config $FtpSiteName /section:system.ftpServer/security/authorization /clear /commit:apphost | Out-Null
    & $appcmd set config $FtpSiteName /section:system.ftpServer/security/authorization "/+[accessType='Allow',users='$TestUser',permissions='Read,Write']" /commit:apphost | Out-Null
    & $appcmd set config $FtpSiteName /section:system.ftpServer/security/authorization "/+[accessType='Allow',roles='Administrators',permissions='Read,Write']" /commit:apphost | Out-Null
    Write-Host "Disabled anonymous auth and enabled basic auth." -ForegroundColor Green
    Write-Host "Added explicit FTP allow rule for user '$TestUser'" -ForegroundColor Green
    Write-Host "Added FTP allow rule for local Administrators role" -ForegroundColor Green
} catch {
    Write-Host "Warning: failed to add authorization rule using appcmd: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "Check FTP Authorization Rules manually in IIS if needed." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Step 5b: Clear location-level FTP overrides..." -ForegroundColor Cyan
Clear-FtpPathOverride -AppCmd $appcmd -LocationPath "$FtpSiteName/$Alias"

Write-Host ""
Write-Host "Step 5c: Recreate FTP authorization specifically for /$Alias..." -ForegroundColor Cyan
try {
    & $appcmd set config "$FtpSiteName/$Alias" /section:system.ftpServer/security/authentication/anonymousAuthentication /enabled:false /commit:apphost | Out-Null
    & $appcmd set config "$FtpSiteName/$Alias" /section:system.ftpServer/security/authentication/basicAuthentication /enabled:true /commit:apphost | Out-Null
    & $appcmd set config "$FtpSiteName/$Alias" /section:system.ftpServer/security/authorization /clear /commit:apphost | Out-Null
    & $appcmd set config "$FtpSiteName/$Alias" /section:system.ftpServer/security/authorization "/+[accessType='Allow',users='$TestUser',permissions='Read,Write']" /commit:apphost | Out-Null
    & $appcmd set config "$FtpSiteName/$Alias" /section:system.ftpServer/security/authorization "/+[accessType='Allow',roles='Administrators',permissions='Read,Write']" /commit:apphost | Out-Null
    Write-Host "Rebuilt location-level FTP authorization for /$Alias" -ForegroundColor Green
} catch {
    Write-Host "Warning: failed to rebuild location-level authorization: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Step 6: Start FTP site..." -ForegroundColor Cyan
& $appcmd start site /site.name:"$FtpSiteName" | Out-Null
Start-Sleep -Seconds 2
Write-Host "FTP site started." -ForegroundColor Green

Write-Host ""
Write-Host "Step 7: Test ftp://localhost/$Alias/..." -ForegroundColor Cyan
$listUriRoot = "ftp://localhost/"
$listUri = "ftp://localhost/$Alias/"

function Test-FtpList {
    param(
        [string]$Uri,
        [string]$User,
        [string]$Password,
        [int]$TimeoutMs
    )

    $request = [System.Net.FtpWebRequest]::Create($Uri)
    $request.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectory
    $request.Credentials = New-Object System.Net.NetworkCredential($User, $Password)
    $request.UsePassive = $true
    $request.UseBinary = $true
    $request.KeepAlive = $false
    $request.Timeout = $TimeoutMs
    $request.ReadWriteTimeout = $TimeoutMs

    $response = [System.Net.FtpWebResponse]$request.GetResponse()
    $reader = New-Object System.IO.StreamReader($response.GetResponseStream())
    $lines = New-Object System.Collections.Generic.List[string]
    for ($i = 0; $i -lt 20; $i++) {
        if ($reader.EndOfStream) { break }
        $lines.Add($reader.ReadLine())
    }
    $reader.Close()
    $response.Close()

    return ($lines -join [Environment]::NewLine)
}

function Invoke-FtpListCheck {
    param(
        [string]$Name,
        [string]$Uri
    )

    try {
        $content = Test-FtpList -Uri $Uri -User $TestUser -Password $TestPassword -TimeoutMs $FtpTimeoutMs
        Write-Host "$Name PASSED: $Uri" -ForegroundColor Green
        if (-not [string]::IsNullOrWhiteSpace($content)) {
            Write-Host "$Name contents:" -ForegroundColor White
            Write-Host $content -ForegroundColor Gray
        }
        return $true
    } catch {
        Write-Host "$Name FAILED: $Uri" -ForegroundColor Red
        if ($_.Exception.Response) {
            $resp = [System.Net.FtpWebResponse]$_.Exception.Response
            Write-Host ("FTP status: {0} {1}" -f [int]$resp.StatusCode, $resp.StatusDescription.Trim()) -ForegroundColor Red
        }
        Write-Host $_.Exception.Message -ForegroundColor Red
        return $false
    }
}

$okRoot = Invoke-FtpListCheck -Name "Root list" -Uri $listUriRoot
$okBank = Invoke-FtpListCheck -Name "BankConsents list" -Uri $listUri

if (-not ($okRoot -and $okBank)) {
    Write-Host "If Root passes but BankConsents fails, this is folder-level FTP access mapping/authorization." -ForegroundColor Yellow
    exit 1
}

if ($TestWrite) {
    Write-Host ""
    Write-Host "Step 8: Test write to ftp://localhost/$Alias/..." -ForegroundColor Cyan
    $bytes = [System.Text.Encoding]::UTF8.GetBytes('ftp write test')
    $writeUri = "ftp://localhost/$Alias/test_$( [guid]::NewGuid().ToString('N') ).txt"
    try {
        $request = [System.Net.FtpWebRequest]::Create($writeUri)
        $request.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
        $request.Credentials = New-Object System.Net.NetworkCredential($TestUser, $TestPassword)
        $request.UsePassive = $true
        $request.UseBinary = $true
        $request.KeepAlive = $false
        $request.ContentLength = $bytes.Length
        $stream = $request.GetRequestStream()
        $stream.Write($bytes, 0, $bytes.Length)
        $stream.Close()
        $response = [System.Net.FtpWebResponse]$request.GetResponse()
        Write-Host "Write test PASSED: $writeUri" -ForegroundColor Green
        Write-Host $response.StatusDescription -ForegroundColor Gray
        $response.Close()
    } catch {
        Write-Host "Write test FAILED: $writeUri" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "Completed successfully." -ForegroundColor Green
Write-Host "If localhost FTP works now, the API can be switched back to /BankConsents." -ForegroundColor Yellow