# FTP Connection Test Script
# This script tests FTP connectivity and directory operations

$ftpHost = "192.168.40.47"
$ftpPort = 21
$ftpUser = "smkcapi_ftp"
$ftpPassword = "S0me`$tr0ngP@ss!"
$basePath = "/BankConsents"

Write-Host "=== FTP Connection Diagnostic Test ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Server: ftp://$ftpHost`:$ftpPort" -ForegroundColor Yellow
Write-Host "User: $ftpUser" -ForegroundColor Yellow
Write-Host "Base Path: $basePath" -ForegroundColor Yellow
Write-Host ""

# Test 1: Network Connectivity
Write-Host "Test 1: Testing network connectivity..." -ForegroundColor Cyan
try {
    $tcpTest = Test-NetConnection -ComputerName $ftpHost -Port $ftpPort -WarningAction SilentlyContinue
    if ($tcpTest.TcpTestSucceeded) {
        Write-Host "? SUCCESS: Can connect to FTP port" -ForegroundColor Green
    } else {
        Write-Host "? FAILED: Cannot connect to FTP port" -ForegroundColor Red
        Write-Host "  Check if FTP service is running on $ftpHost" -ForegroundColor Yellow
        exit 1
    }
} catch {
    Write-Host "? FAILED: Network test error - $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Test 2: FTP Authentication & List Root
Write-Host "Test 2: Testing FTP authentication (list root directory)..." -ForegroundColor Cyan
try {
    $ftpUri = "ftp://$ftpHost`:$ftpPort/"
    $request = [System.Net.FtpWebRequest]::Create($ftpUri)
    $request.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectory
    $request.Credentials = New-Object System.Net.NetworkCredential($ftpUser, $ftpPassword)
    $request.UsePassive = $true
    $request.UseBinary = $true
    $request.KeepAlive = $false
    $request.Timeout = 10000

    $response = $request.GetResponse()
    $stream = $response.GetResponseStream()
    $reader = New-Object System.IO.StreamReader($stream)
    $listing = $reader.ReadToEnd()
    $reader.Close()
    $response.Close()

    Write-Host "? SUCCESS: Authentication successful" -ForegroundColor Green
    Write-Host "Root directory contents:" -ForegroundColor Gray
    Write-Host $listing -ForegroundColor Gray
} catch [System.Net.WebException] {
    $ftpResponse = $_.Exception.Response
    if ($ftpResponse) {
        Write-Host "? FAILED: FTP Error - Status: $($ftpResponse.StatusCode), Message: $($ftpResponse.StatusDescription)" -ForegroundColor Red
        if ($ftpResponse.StatusCode -eq 530) {
            Write-Host "  ? Authentication failed. Check username/password in Web.config" -ForegroundColor Yellow
        }
    } else {
        Write-Host "? FAILED: Connection error - $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "  ? Cannot reach FTP server. Check if FTP service is running." -ForegroundColor Yellow
    }
    exit 1
} catch {
    Write-Host "? FAILED: Unexpected error - $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Test 3: Check if /BankConsents exists
Write-Host "Test 3: Checking if $basePath directory exists..." -ForegroundColor Cyan
try {
    $ftpUri = "ftp://$ftpHost`:$ftpPort$basePath"
    $request = [System.Net.FtpWebRequest]::Create($ftpUri)
    $request.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectory
    $request.Credentials = New-Object System.Net.NetworkCredential($ftpUser, $ftpPassword)
    $request.UsePassive = $true
    $request.UseBinary = $true
    $request.KeepAlive = $false
    $request.Timeout = 10000

    $response = $request.GetResponse()
    $response.Close()
    
    Write-Host "? SUCCESS: Directory $basePath exists" -ForegroundColor Green
    $directoryExists = $true
} catch [System.Net.WebException] {
    $ftpResponse = $_.Exception.Response
    if ($ftpResponse -and $ftpResponse.StatusCode -eq 550) {
        Write-Host "? WARNING: Directory $basePath does not exist (550)" -ForegroundColor Yellow
        $directoryExists = $false
    } else {
        Write-Host "? FAILED: Error checking directory - $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "? FAILED: Unexpected error - $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Test 4: Try to create /BankConsents if it doesn't exist
if (-not $directoryExists) {
    Write-Host "Test 4: Attempting to create $basePath directory..." -ForegroundColor Cyan
    try {
        $ftpUri = "ftp://$ftpHost`:$ftpPort$basePath"
        $request = [System.Net.FtpWebRequest]::Create($ftpUri)
        $request.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
        $request.Credentials = New-Object System.Net.NetworkCredential($ftpUser, $ftpPassword)
        $request.UsePassive = $true
        $request.UseBinary = $true
        $request.KeepAlive = $false
        $request.Timeout = 10000

        $response = $request.GetResponse()
        Write-Host "? SUCCESS: Created directory $basePath" -ForegroundColor Green
        Write-Host "  Status: $($response.StatusDescription)" -ForegroundColor Gray
        $response.Close()
    } catch [System.Net.WebException] {
        $ftpResponse = $_.Exception.Response
        if ($ftpResponse) {
            Write-Host "? FAILED: Cannot create directory - Status: $($ftpResponse.StatusCode), Message: $($ftpResponse.StatusDescription)" -ForegroundColor Red
            if ($ftpResponse.StatusCode -eq 550) {
                Write-Host "  ? PERMISSION DENIED: FTP user '$ftpUser' needs write permissions" -ForegroundColor Yellow
                Write-Host "  ? ACTION: On FTP server, grant Full Control to user '$ftpUser' on FTP root directory" -ForegroundColor Yellow
            }
        } else {
            Write-Host "? FAILED: Connection error - $($_.Exception.Message)" -ForegroundColor Red
        }
        exit 1
    } catch {
        Write-Host "? FAILED: Unexpected error - $_" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "Test 4: Skipped (directory already exists)" -ForegroundColor Gray
}

Write-Host ""

# Test 5: Test write permissions with a test subdirectory
Write-Host "Test 5: Testing write permissions (create/delete test directory)..." -ForegroundColor Cyan
$testDirName = "TEST_" + [Guid]::NewGuid().ToString("N").Substring(0, 8)
$testDir = "$basePath/$testDirName"
$testDirCreated = $false

try {
    # Create test directory
    $ftpUri = "ftp://$ftpHost`:$ftpPort$testDir"
    $request = [System.Net.FtpWebRequest]::Create($ftpUri)
    $request.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
    $request.Credentials = New-Object System.Net.NetworkCredential($ftpUser, $ftpPassword)
    $request.UsePassive = $true
    $request.UseBinary = $true
    $request.KeepAlive = $false
    $request.Timeout = 10000

    $response = $request.GetResponse()
    Write-Host "? SUCCESS: Created test directory $testDir" -ForegroundColor Green
    $response.Close()
    $testDirCreated = $true
    
    # Clean up - delete test directory
    $request = [System.Net.FtpWebRequest]::Create($ftpUri)
    $request.Method = [System.Net.WebRequestMethods+Ftp]::RemoveDirectory
    $request.Credentials = New-Object System.Net.NetworkCredential($ftpUser, $ftpPassword)
    $request.UsePassive = $true
    $request.UseBinary = $true
    $request.KeepAlive = $false
    $request.Timeout = 10000

    $response = $request.GetResponse()
    Write-Host "? SUCCESS: Deleted test directory $testDir" -ForegroundColor Green
    $response.Close()
} catch [System.Net.WebException] {
    $ftpResponse = $_.Exception.Response
    if ($ftpResponse) {
        Write-Host "? FAILED: Status: $($ftpResponse.StatusCode), Message: $($ftpResponse.StatusDescription)" -ForegroundColor Red
        if ($ftpResponse.StatusCode -eq 550) {
            Write-Host "  ? PERMISSION DENIED: FTP user '$ftpUser' needs write/modify permissions on $basePath" -ForegroundColor Yellow
        }
    } else {
        Write-Host "? FAILED: Connection error - $($_.Exception.Message)" -ForegroundColor Red
    }
    
    # Try to clean up if directory was created
    if ($testDirCreated) {
        try {
            $ftpUri = "ftp://$ftpHost`:$ftpPort$testDir"
            $request = [System.Net.FtpWebRequest]::Create($ftpUri)
            $request.Method = [System.Net.WebRequestMethods+Ftp]::RemoveDirectory
            $request.Credentials = New-Object System.Net.NetworkCredential($ftpUser, $ftpPassword)
            $request.Timeout = 5000
            $request.GetResponse().Close()
            Write-Host "  (Cleaned up test directory)" -ForegroundColor Gray
        } catch {
            Write-Host "  (Warning: Could not clean up $testDir - please delete manually)" -ForegroundColor Yellow
        }
    }
    exit 1
} catch {
    Write-Host "? FAILED: Unexpected error - $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=== ALL TESTS PASSED ===" -ForegroundColor Green
Write-Host "FTP server is accessible and properly configured!" -ForegroundColor Green
Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  ? Network connectivity: OK" -ForegroundColor Green
Write-Host "  ? FTP authentication: OK" -ForegroundColor Green
Write-Host "  ? Base directory: Exists" -ForegroundColor Green
Write-Host "  ? Write permissions: OK" -ForegroundColor Green
Write-Host ""
Write-Host "You can now upload consent documents successfully!" -ForegroundColor Green
