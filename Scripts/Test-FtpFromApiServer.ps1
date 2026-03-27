# ???????????????????????????????????????????????????????????
# ?? FTP Connection Test from API Server
# Run this script on the API SERVER (192.168.40.35)
# ???????????????????????????????????????????????????????????

Write-Host "???????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?  FTP Connection Test from API Server                     ?" -ForegroundColor Cyan
Write-Host "???????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

$ftpServerIP = "192.168.40.47"
$ftpPort = 21

# ==============================================================================
# 1. BASIC NETWORK TEST
# ==============================================================================
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "1. Basic Network Connectivity" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray

Write-Host "  Testing PING to $ftpServerIP..." -ForegroundColor White
try {
    $pingResult = Test-Connection -ComputerName $ftpServerIP -Count 2 -ErrorAction Stop
    Write-Host "  ? PING: SUCCESS" -ForegroundColor Green
    Write-Host "    Average Response Time: $([math]::Round(($pingResult | Measure-Object -Property ResponseTime -Average).Average))ms" -ForegroundColor White
} catch {
    Write-Host "  ? PING: FAILED" -ForegroundColor Red
    Write-Host "  ? Check network connectivity" -ForegroundColor Yellow
}
Write-Host ""

# ==============================================================================
# 2. TCP PORT 21 TEST
# ==============================================================================
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "2. TCP Port 21 Connection Test" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray

Write-Host "  Testing TCP connection to $ftpServerIP`:$ftpPort..." -ForegroundColor White
try {
    $tcpTest = Test-NetConnection -ComputerName $ftpServerIP -Port $ftpPort -InformationLevel Detailed -WarningAction SilentlyContinue
    
    Write-Host ""
    Write-Host "  Results:" -ForegroundColor White
    Write-Host "    Computer Name: $($tcpTest.ComputerName)" -ForegroundColor Gray
    Write-Host "    Remote Address: $($tcpTest.RemoteAddress)" -ForegroundColor Gray
    Write-Host "    Remote Port: $($tcpTest.RemotePort)" -ForegroundColor Gray
    Write-Host "    Interface Alias: $($tcpTest.InterfaceAlias)" -ForegroundColor Gray
    Write-Host "    Source Address: $($tcpTest.SourceAddress)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "    Ping Succeeded: $($tcpTest.PingSucceeded)" -ForegroundColor $(if($tcpTest.PingSucceeded){'Green'}else{'Yellow'})
    Write-Host "    TCP Test Succeeded: $($tcpTest.TcpTestSucceeded)" -ForegroundColor $(if($tcpTest.TcpTestSucceeded){'Green'}else{'Red'})
    Write-Host ""
    
    if ($tcpTest.TcpTestSucceeded) {
        Write-Host "  ? TCP CONNECTION: SUCCESS" -ForegroundColor Green
        Write-Host "    ? FTP server is reachable on port 21" -ForegroundColor Green
        Write-Host "    ? Firewall is configured correctly" -ForegroundColor Green
    } else {
        Write-Host "  ? TCP CONNECTION: FAILED" -ForegroundColor Red
        Write-Host ""
        Write-Host "  This means:" -ForegroundColor Yellow
        Write-Host "    1. Firewall on FTP server is blocking port 21, OR" -ForegroundColor White
        Write-Host "    2. FTP service is not listening on port 21, OR" -ForegroundColor White
        Write-Host "    3. Network infrastructure is blocking the connection" -ForegroundColor White
        Write-Host ""
        Write-Host "  Next Steps:" -ForegroundColor Red
        Write-Host "    ? On FTP Server, run: Scripts\Diagnose-FtpServerIssue.ps1" -ForegroundColor Yellow
        Write-Host "    ? Verify port 21 is listening: netstat -ano | findstr :21" -ForegroundColor Yellow
        Write-Host "    ? Check firewall rules exist and are enabled" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  ? Error during TCP test: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# ==============================================================================
# 3. ROUTE TRACE
# ==============================================================================
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "3. Network Route Trace" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray

Write-Host "  Tracing route to $ftpServerIP..." -ForegroundColor White
try {
    $traceResult = Test-NetConnection -ComputerName $ftpServerIP -TraceRoute -WarningAction SilentlyContinue
    
    if ($traceResult.TraceRoute) {
        Write-Host "  Hops:" -ForegroundColor White
        $hopNumber = 1
        foreach ($hop in $traceResult.TraceRoute) {
            Write-Host "    $hopNumber. $hop" -ForegroundColor Gray
            $hopNumber++
        }
        
        $hopCount = $traceResult.TraceRoute.Count
        if ($hopCount -eq 1) {
            Write-Host "  ? Direct connection (same network segment)" -ForegroundColor Green
        } elseif ($hopCount -le 3) {
            Write-Host "  ? Few hops ($hopCount) - likely on same internal network" -ForegroundColor Green
        } else {
            Write-Host "  ? Multiple hops ($hopCount) detected" -ForegroundColor Yellow
            Write-Host "  ? May need network team to check routers/switches" -ForegroundColor Yellow
        }
    }
} catch {
    Write-Host "  ? Could not trace route: $($_.Exception.Message)" -ForegroundColor Yellow
}
Write-Host ""

# ==============================================================================
# 4. TELNET TEST (if available)
# ==============================================================================
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "4. FTP Protocol Test (Telnet)" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray

Write-Host "  Attempting raw TCP connection to FTP port..." -ForegroundColor White
try {
    $tcpClient = New-Object System.Net.Sockets.TcpClient
    $asyncResult = $tcpClient.BeginConnect($ftpServerIP, $ftpPort, $null, $null)
    $wait = $asyncResult.AsyncWaitHandle.WaitOne(5000, $false)
    
    if ($wait) {
        try {
            $tcpClient.EndConnect($asyncResult)
            
            if ($tcpClient.Connected) {
                Write-Host "  ? RAW TCP CONNECTION: SUCCESS" -ForegroundColor Green
                
                # Try to read FTP banner
                $stream = $tcpClient.GetStream()
                $stream.ReadTimeout = 5000
                $reader = New-Object System.IO.StreamReader($stream)
                
                try {
                    $banner = $reader.ReadLine()
                    Write-Host "  ? FTP BANNER RECEIVED: $banner" -ForegroundColor Green
                    Write-Host "    ? FTP server is responding correctly!" -ForegroundColor Green
                } catch {
                    Write-Host "  ? Connected but no FTP banner received" -ForegroundColor Yellow
                }
                
                $tcpClient.Close()
            } else {
                Write-Host "  ? TCP CONNECTION: FAILED" -ForegroundColor Red
            }
        } catch {
            Write-Host "  ? TCP CONNECTION: FAILED - $($_.Exception.Message)" -ForegroundColor Red
        }
    } else {
        Write-Host "  ? TCP CONNECTION: TIMEOUT" -ForegroundColor Red
        Write-Host "  ? Connection attempt timed out after 5 seconds" -ForegroundColor Yellow
        $tcpClient.Close()
    }
} catch {
    Write-Host "  ? Error: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# ==============================================================================
# 5. FTP WEB REQUEST TEST
# ==============================================================================
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "5. FTP WebRequest Test (Passive Mode)" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray

Write-Host "  ? This test requires FTP credentials" -ForegroundColor Yellow
Write-Host "  Enter FTP username (or press Enter to skip): " -NoNewline
$ftpUser = Read-Host

if ($ftpUser) {
    Write-Host "  Enter FTP password: " -NoNewline
    $ftpPassword = Read-Host -AsSecureString
    $ftpPasswordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($ftpPassword)
    )
    
    Write-Host ""
    Write-Host "  Testing FTP connection with credentials..." -ForegroundColor White
    
    try {
        $ftpUri = "ftp://$ftpServerIP/"
        $ftpRequest = [System.Net.FtpWebRequest]::Create($ftpUri)
        $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectory
        $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($ftpUser, $ftpPasswordPlain)
        $ftpRequest.Timeout = 30000
        $ftpRequest.UsePassive = $true
        $ftpRequest.UseBinary = $true
        $ftpRequest.KeepAlive = $false
        
        Write-Host "  ? Connecting to FTP server..." -ForegroundColor Gray
        $response = $ftpRequest.GetResponse()
        
        Write-Host "  ? FTP CONNECTION: SUCCESS" -ForegroundColor Green
        Write-Host "    Status: $($response.StatusDescription)" -ForegroundColor Green
        Write-Host "    Welcome Message: $($response.WelcomeMessage)" -ForegroundColor Gray
        Write-Host ""
        Write-Host "  ??? FTP IS FULLY WORKING! ???" -ForegroundColor Green
        
        $response.Close()
    } catch {
        Write-Host "  ? FTP CONNECTION: FAILED" -ForegroundColor Red
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
        
        if ($_.Exception.Message -like "*timeout*") {
            Write-Host "  Timeout indicates:" -ForegroundColor Yellow
            Write-Host "    - Firewall blocking connection, OR" -ForegroundColor White
            Write-Host "    - FTP service not responding, OR" -ForegroundColor White
            Write-Host "    - Passive mode ports blocked" -ForegroundColor White
        } elseif ($_.Exception.Message -like "*530*") {
            Write-Host "  Error 530 indicates:" -ForegroundColor Yellow
            Write-Host "    - Invalid username or password" -ForegroundColor White
            Write-Host "    - FTP authentication not configured" -ForegroundColor White
        } elseif ($_.Exception.Message -like "*550*") {
            Write-Host "  Error 550 indicates:" -ForegroundColor Yellow
            Write-Host "    - Permission denied" -ForegroundColor White
            Write-Host "    - Directory not accessible" -ForegroundColor White
        }
    }
} else {
    Write-Host "  ? FTP protocol test skipped (no credentials provided)" -ForegroundColor Gray
}
Write-Host ""

# ==============================================================================
# 6. CHECK FIREWALL ON THIS MACHINE (API SERVER)
# ==============================================================================
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "6. Local Firewall Check (API Server)" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray

Write-Host "  Checking for outbound firewall blocks..." -ForegroundColor White
$blockingOutbound = Get-NetFirewallRule -Direction Outbound -Action Block -Enabled True | 
    Get-NetFirewallPortFilter | 
    Where-Object {$_.RemotePort -eq 21 -or $_.RemotePort -eq 'Any'}

if ($blockingOutbound) {
    Write-Host "  ? Found blocking OUTBOUND rules:" -ForegroundColor Red
    foreach ($filter in $blockingOutbound) {
        $rule = $filter | Get-NetFirewallRule
        Write-Host "    - $($rule.DisplayName)" -ForegroundColor Red
    }
    Write-Host "  ? These may prevent FTP connections" -ForegroundColor Yellow
} else {
    Write-Host "  ? No blocking outbound firewall rules for FTP" -ForegroundColor Green
}
Write-Host ""

# ==============================================================================
# 7. SUMMARY & RECOMMENDATIONS
# ==============================================================================
Write-Host "???????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?  TEST SUMMARY                                            ?" -ForegroundColor Cyan
Write-Host "???????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

# Determine overall status
$tcpTestFinal = Test-NetConnection -ComputerName $ftpServerIP -Port $ftpPort -WarningAction SilentlyContinue

Write-Host "Current Status:" -ForegroundColor White
Write-Host "  API Server IP: $((Get-NetIPAddress -AddressFamily IPv4 | Where-Object {$_.IPAddress -like '192.168.*'}).IPAddress)" -ForegroundColor Gray
Write-Host "  Target FTP Server: $ftpServerIP`:$ftpPort" -ForegroundColor Gray
Write-Host ""

if ($tcpTestFinal.PingSucceeded) {
    Write-Host "  ? Network Layer: Working (PING succeeds)" -ForegroundColor Green
} else {
    Write-Host "  ? Network Layer: Issues detected" -ForegroundColor Red
}

if ($tcpTestFinal.TcpTestSucceeded) {
    Write-Host "  ? Transport Layer: Working (TCP port 21 reachable)" -ForegroundColor Green
    Write-Host ""
    Write-Host "  ?? SUCCESS! FTP server is reachable!" -ForegroundColor Green
    Write-Host ""
    Write-Host "  If your application still can't connect:" -ForegroundColor Yellow
    Write-Host "    1. Check FTP credentials in app configuration" -ForegroundColor White
    Write-Host "    2. Verify passive mode is enabled in FTP client" -ForegroundColor White
    Write-Host "    3. Check FTP user permissions on server" -ForegroundColor White
    Write-Host "    4. Review application logs for specific error messages" -ForegroundColor White
} else {
    Write-Host "  ? Transport Layer: BLOCKED (TCP port 21 not reachable)" -ForegroundColor Red
    Write-Host ""
    Write-Host "  ?? CRITICAL: FTP server port 21 is NOT accessible!" -ForegroundColor Red
    Write-Host ""
    Write-Host "  Possible Causes (in order of likelihood):" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  1. ????? FTP Service Not Running or Port Not Listening" -ForegroundColor White
    Write-Host "     ? On FTP Server: netstat -ano | findstr :21" -ForegroundColor Cyan
    Write-Host "     ? Should show: TCP 0.0.0.0:21 ... LISTENING" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  2. ????? FTP Site Not Started in IIS" -ForegroundColor White
    Write-Host "     ? On FTP Server: Open IIS Manager" -ForegroundColor Cyan
    Write-Host "     ? Sites ? [FTP Site] ? Start" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  3. ???? Firewall Blocking on FTP Server" -ForegroundColor White
    Write-Host "     ? On FTP Server: Run Fix-FtpServer-Complete.ps1" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  4. ??? Network Infrastructure (Router/Switch/VLAN)" -ForegroundColor White
    Write-Host "     ? Contact network team" -ForegroundColor Cyan
    Write-Host "     ? Check ACLs between 192.168.40.35 and 192.168.40.47" -ForegroundColor Cyan
    Write-Host ""
}
Write-Host ""

# ==============================================================================
# 8. NEXT STEPS
# ==============================================================================
Write-Host "???????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?  RECOMMENDED NEXT STEPS                                  ?" -ForegroundColor Cyan
Write-Host "???????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

if (-not $tcpTestFinal.TcpTestSucceeded) {
    Write-Host "? CONNECTION FAILED - Action Required:" -ForegroundColor Red
    Write-Host ""
    Write-Host "On FTP Server (192.168.40.47):" -ForegroundColor Yellow
    Write-Host "  1. Run deep diagnostics:" -ForegroundColor White
    Write-Host "     .\Scripts\Diagnose-FtpServerIssue.ps1" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  2. Run complete fix script:" -ForegroundColor White
    Write-Host "     .\Scripts\Fix-FtpServer-Complete.ps1" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  3. Verify locally:" -ForegroundColor White
    Write-Host "     Test-NetConnection -ComputerName localhost -Port 21" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  4. Check if port 21 is listening:" -ForegroundColor White
    Write-Host "     netstat -ano | findstr :21" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Then re-run this test script from API server." -ForegroundColor Yellow
} else {
    Write-Host "? CONNECTION SUCCESSFUL - FTP is reachable!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Test your application now:" -ForegroundColor Yellow
    Write-Host "  curl -k https://localhost:5443/api/ftp-diagnostic/network-info" -ForegroundColor Cyan
    Write-Host "  curl -k https://localhost:5443/api/ftp-diagnostic/test" -ForegroundColor Cyan
}
Write-Host ""

Write-Host "???????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "Test completed at $(Get-Date)" -ForegroundColor Green
Write-Host "???????????????????????????????????????????????????????????" -ForegroundColor Cyan
