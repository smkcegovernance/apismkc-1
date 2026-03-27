# ???????????????????????????????????????????????????????????
# ?? FTP Server Deep Diagnostics
# Run this script as Administrator on the FTP SERVER (192.168.40.47)
# ???????????????????????????????????????????????????????????

Write-Host "???????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?  FTP Server Deep Diagnostics                             ?" -ForegroundColor Cyan
Write-Host "???????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

$apiServerIP = "192.168.40.35"

# Check if running as Administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
$isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "? ERROR: This script must be run as Administrator!" -ForegroundColor Red
    exit 1
}

Write-Host "? Running as Administrator" -ForegroundColor Green
Write-Host ""

# ==============================================================================
# 1. FTP SERVICE CHECK
# ==============================================================================
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "1. FTP Service Status" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray

$ftpService = Get-Service ftpsvc -ErrorAction SilentlyContinue
if ($ftpService) {
    Write-Host "  Service Name: $($ftpService.Name)" -ForegroundColor White
    Write-Host "  Display Name: $($ftpService.DisplayName)" -ForegroundColor White
    Write-Host "  Status: $($ftpService.Status)" -ForegroundColor $(if($ftpService.Status -eq 'Running'){'Green'}else{'Red'})
    Write-Host "  Start Type: $($ftpService.StartType)" -ForegroundColor White
} else {
    Write-Host "  ? FTP service (ftpsvc) NOT INSTALLED!" -ForegroundColor Red
    Write-Host "  ? Install IIS FTP Server role" -ForegroundColor Yellow
    exit 1
}
Write-Host ""

# ==============================================================================
# 2. PORT LISTENING CHECK
# ==============================================================================
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "2. Port Listening Check" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray

$listening = netstat -ano | Select-String ":21.*LISTENING"
if ($listening) {
    Write-Host "  ? Port 21 is LISTENING" -ForegroundColor Green
    foreach ($line in $listening) {
        Write-Host "    $line" -ForegroundColor Gray
    }
} else {
    Write-Host "  ? Port 21 is NOT LISTENING!" -ForegroundColor Red
    Write-Host "  ? FTP site may not be started in IIS Manager" -ForegroundColor Yellow
}
Write-Host ""

# ==============================================================================
# 3. FIREWALL PROFILE STATUS
# ==============================================================================
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "3. Windows Firewall Profiles" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray

$profiles = Get-NetFirewallProfile | Select-Object Name, Enabled, DefaultInboundAction, DefaultOutboundAction
foreach ($profile in $profiles) {
    $status = if ($profile.Enabled) { "Enabled" } else { "Disabled" }
    $color = if ($profile.Enabled) { "Yellow" } else { "Green" }
    Write-Host "  [$($profile.Name) Profile]" -ForegroundColor White
    Write-Host "    Firewall: $status" -ForegroundColor $color
    Write-Host "    Default Inbound: $($profile.DefaultInboundAction)" -ForegroundColor $(if($profile.DefaultInboundAction -eq 'Block'){'Red'}else{'Green'})
    Write-Host "    Default Outbound: $($profile.DefaultOutboundAction)" -ForegroundColor White
}
Write-Host ""

# ==============================================================================
# 4. ACTIVE FIREWALL PROFILE
# ==============================================================================
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "4. Currently Active Firewall Profile" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray

$activeProfiles = Get-NetConnectionProfile
if ($activeProfiles) {
    foreach ($profile in $activeProfiles) {
        Write-Host "  Interface: $($profile.InterfaceAlias)" -ForegroundColor White
        Write-Host "  Network Category: $($profile.NetworkCategory)" -ForegroundColor Yellow
        Write-Host "  ? This determines which firewall profile is active!" -ForegroundColor Magenta
    }
} else {
    Write-Host "  ? Could not determine active network profile" -ForegroundColor Yellow
}
Write-Host ""

# ==============================================================================
# 5. FTP FIREWALL RULES CHECK
# ==============================================================================
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "5. FTP Firewall Rules" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray

# Check custom rules
$ftpControlRule = Get-NetFirewallRule -DisplayName "FTP - Allow from SMKCAPI" -ErrorAction SilentlyContinue
$ftpPassiveRule = Get-NetFirewallRule -DisplayName "FTP - Passive Mode Data" -ErrorAction SilentlyContinue

if ($ftpControlRule) {
    Write-Host "  ? Custom FTP Control Rule EXISTS" -ForegroundColor Green
    Write-Host "    Name: $($ftpControlRule.DisplayName)" -ForegroundColor White
    Write-Host "    Enabled: $($ftpControlRule.Enabled)" -ForegroundColor $(if($ftpControlRule.Enabled -eq 'True'){'Green'}else{'Red'})
    Write-Host "    Action: $($ftpControlRule.Action)" -ForegroundColor White
    Write-Host "    Direction: $($ftpControlRule.Direction)" -ForegroundColor White
    Write-Host "    Profile: $($ftpControlRule.Profile)" -ForegroundColor Yellow
    
    $portFilter = $ftpControlRule | Get-NetFirewallPortFilter
    $addressFilter = $ftpControlRule | Get-NetFirewallAddressFilter
    Write-Host "    Local Port: $($portFilter.LocalPort)" -ForegroundColor White
    Write-Host "    Protocol: $($portFilter.Protocol)" -ForegroundColor White
    Write-Host "    Remote Address: $($addressFilter.RemoteAddress)" -ForegroundColor White
} else {
    Write-Host "  ? Custom FTP Control Rule NOT FOUND!" -ForegroundColor Red
    Write-Host "  ? Run Fix-FtpServer-Firewall.ps1 to create it" -ForegroundColor Yellow
}
Write-Host ""

if ($ftpPassiveRule) {
    Write-Host "  ? Custom FTP Passive Rule EXISTS" -ForegroundColor Green
    Write-Host "    Name: $($ftpPassiveRule.DisplayName)" -ForegroundColor White
    Write-Host "    Enabled: $($ftpPassiveRule.Enabled)" -ForegroundColor $(if($ftpPassiveRule.Enabled -eq 'True'){'Green'}else{'Red'})
    Write-Host "    Profile: $($ftpPassiveRule.Profile)" -ForegroundColor Yellow
    
    $portFilter = $ftpPassiveRule | Get-NetFirewallPortFilter
    Write-Host "    Local Port Range: $($portFilter.LocalPort)" -ForegroundColor White
} else {
    Write-Host "  ? Custom FTP Passive Rule NOT FOUND!" -ForegroundColor Red
}
Write-Host ""

# Check all FTP related rules
Write-Host "  All FTP-related firewall rules:" -ForegroundColor Cyan
$allFtpRules = Get-NetFirewallRule | Where-Object {$_.DisplayName -like "*FTP*"}
if ($allFtpRules) {
    foreach ($rule in $allFtpRules) {
        $color = if ($rule.Enabled -eq 'True') { 'White' } else { 'DarkGray' }
        Write-Host "    - $($rule.DisplayName) [$($rule.Direction)] [$($rule.Action)] [Profile: $($rule.Profile)]" -ForegroundColor $color
        Write-Host "      Enabled: $($rule.Enabled)" -ForegroundColor $color
    }
} else {
    Write-Host "    ? No FTP firewall rules found!" -ForegroundColor Yellow
}
Write-Host ""

# ==============================================================================
# 6. IIS FTP CONFIGURATION
# ==============================================================================
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "6. IIS FTP Configuration" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray

try {
    Import-Module WebAdministration -ErrorAction Stop
    
    $ftpSites = Get-WebConfiguration -Filter "system.applicationHost/sites/site" | 
        Where-Object { $_.bindings.Collection.protocol -contains "ftp" }
    
    if ($ftpSites) {
        Write-Host "  ? FTP Sites Found:" -ForegroundColor Green
        foreach ($site in $ftpSites) {
            Write-Host "    Site Name: $($site.name)" -ForegroundColor White
            Write-Host "    State: $($site.state)" -ForegroundColor $(if($site.state -eq 'Started'){'Green'}else{'Red'})
            
            # Get bindings
            foreach ($binding in $site.bindings.Collection) {
                if ($binding.protocol -eq "ftp") {
                    Write-Host "    Binding: $($binding.bindingInformation)" -ForegroundColor White
                }
            }
        }
    } else {
        Write-Host "  ? No FTP sites found in IIS!" -ForegroundColor Red
        Write-Host "  ? Create an FTP site in IIS Manager" -ForegroundColor Yellow
    }
    
    # Check FTP Firewall Support
    Write-Host ""
    Write-Host "  FTP Firewall Support Configuration:" -ForegroundColor Cyan
    $ftpFirewall = Get-WebConfigurationProperty -pspath 'MACHINE/WEBROOT/APPHOST' -filter "system.ftpServer/firewallSupport" -name "."
    if ($ftpFirewall) {
        Write-Host "    External IP: $($ftpFirewall.externalIp4Address)" -ForegroundColor $(if($ftpFirewall.externalIp4Address){'White'}else{'Yellow'})
        Write-Host "    Low Data Port: $($ftpFirewall.lowDataChannelPort)" -ForegroundColor White
        Write-Host "    High Data Port: $($ftpFirewall.highDataChannelPort)" -ForegroundColor White
        
        if (-not $ftpFirewall.lowDataChannelPort -or -not $ftpFirewall.highDataChannelPort) {
            Write-Host "    ? WARNING: Data port range not configured!" -ForegroundColor Yellow
            Write-Host "    ? This can cause passive mode connections to fail" -ForegroundColor Yellow
        }
    }
} catch {
    Write-Host "  ? Could not load IIS configuration: $($_.Exception.Message)" -ForegroundColor Yellow
}
Write-Host ""

# ==============================================================================
# 7. NETWORK CONNECTIVITY TEST
# ==============================================================================
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "7. Network Connectivity Tests" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray

# Test local connection
Write-Host "  Testing local FTP connection (localhost:21)..." -ForegroundColor White
try {
    $localTest = Test-NetConnection -ComputerName localhost -Port 21 -WarningAction SilentlyContinue
    if ($localTest.TcpTestSucceeded) {
        Write-Host "    ? Local connection: SUCCESS" -ForegroundColor Green
    } else {
        Write-Host "    ? Local connection: FAILED" -ForegroundColor Red
        Write-Host "    ? FTP service may not be configured correctly" -ForegroundColor Yellow
    }
} catch {
    Write-Host "    ? Error: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test from API server perspective (if possible)
Write-Host "  Can API server ($apiServerIP) reach this FTP server?" -ForegroundColor White
Write-Host "    ? You'll need to run Test-NetConnection on API server" -ForegroundColor Yellow
Write-Host ""

# ==============================================================================
# 8. NETWORK ADAPTER & ROUTING
# ==============================================================================
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "8. Network Adapter & IP Configuration" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray

$adapters = Get-NetAdapter | Where-Object {$_.Status -eq 'Up'}
foreach ($adapter in $adapters) {
    Write-Host "  Interface: $($adapter.Name)" -ForegroundColor White
    Write-Host "    Status: $($adapter.Status)" -ForegroundColor Green
    
    $ipConfig = Get-NetIPAddress -InterfaceAlias $adapter.Name -AddressFamily IPv4 -ErrorAction SilentlyContinue
    if ($ipConfig) {
        Write-Host "    IP Address: $($ipConfig.IPAddress)" -ForegroundColor White
        
        if ($ipConfig.IPAddress -eq "192.168.40.47") {
            Write-Host "    ? This is the FTP server IP!" -ForegroundColor Green
        }
    }
}
Write-Host ""

# ==============================================================================
# 9. BLOCKING RULES CHECK
# ==============================================================================
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "9. Checking for BLOCKING Firewall Rules" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray

$blockingRules = Get-NetFirewallRule | Where-Object {
    $_.Enabled -eq 'True' -and 
    $_.Direction -eq 'Inbound' -and 
    $_.Action -eq 'Block'
} | Select-Object -First 10

if ($blockingRules) {
    Write-Host "  Active BLOCKING rules found (showing first 10):" -ForegroundColor Yellow
    foreach ($rule in $blockingRules) {
        $portFilter = $rule | Get-NetFirewallPortFilter -ErrorAction SilentlyContinue
        if ($portFilter -and ($portFilter.LocalPort -eq '21' -or $portFilter.LocalPort -eq 'Any')) {
            Write-Host "    ? $($rule.DisplayName) [Profile: $($rule.Profile)]" -ForegroundColor Red
            Write-Host "      Port: $($portFilter.LocalPort), Protocol: $($portFilter.Protocol)" -ForegroundColor Red
        }
    }
} else {
    Write-Host "  ? No blocking rules found" -ForegroundColor Green
}
Write-Host ""

# ==============================================================================
# 10. FTP AUTHENTICATION & AUTHORIZATION
# ==============================================================================
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "10. FTP Authentication Configuration" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray

try {
    $ftpAuth = Get-WebConfiguration -pspath 'MACHINE/WEBROOT/APPHOST' -filter "system.ftpServer/security/authentication/*"
    
    Write-Host "  Authentication Methods:" -ForegroundColor White
    foreach ($auth in $ftpAuth) {
        $authName = $auth.ElementTagName
        $enabled = $auth.enabled
        $color = if ($enabled) { 'Green' } else { 'Gray' }
        Write-Host "    $authName`: $enabled" -ForegroundColor $color
    }
} catch {
    Write-Host "  ? Could not read FTP authentication config" -ForegroundColor Yellow
}
Write-Host ""

# ==============================================================================
# 11. FTP SSL/TLS CONFIGURATION
# ==============================================================================
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "11. FTP SSL/TLS Configuration" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray

try {
    $sslConfig = Get-WebConfiguration -pspath 'MACHINE/WEBROOT/APPHOST' -filter "system.ftpServer/security/ssl"
    Write-Host "  SSL Policy: $($sslConfig.controlChannelPolicy)" -ForegroundColor White
    Write-Host "  Data Channel: $($sslConfig.dataChannelPolicy)" -ForegroundColor White
    
    if ($sslConfig.controlChannelPolicy -eq 'SslRequire') {
        Write-Host "  ? SSL is REQUIRED - ensure your FTP client supports FTPS" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  ? Could not read SSL configuration" -ForegroundColor Yellow
}
Write-Host ""

# ==============================================================================
# 12. TEST FTP CONNECTION WITH DETAILED OUTPUT
# ==============================================================================
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "12. Detailed Connection Test" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray

Write-Host "  Testing TCP connection to localhost:21..." -ForegroundColor White
try {
    $tcpTest = Test-NetConnection -ComputerName localhost -Port 21 -InformationLevel Detailed -WarningAction SilentlyContinue
    
    Write-Host "    Computer Name: $($tcpTest.ComputerName)" -ForegroundColor White
    Write-Host "    Remote Address: $($tcpTest.RemoteAddress)" -ForegroundColor White
    Write-Host "    Remote Port: $($tcpTest.RemotePort)" -ForegroundColor White
    Write-Host "    TCP Test: $($tcpTest.TcpTestSucceeded)" -ForegroundColor $(if($tcpTest.TcpTestSucceeded){'Green'}else{'Red'})
    Write-Host "    Ping Success: $($tcpTest.PingSucceeded)" -ForegroundColor $(if($tcpTest.PingSucceeded){'Green'}else{'Red'})
    
} catch {
    Write-Host "    ? Connection test failed: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# ==============================================================================
# 13. WINDOWS FIREWALL LOG ANALYSIS
# ==============================================================================
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "13. Recent Firewall Drops (Last 10)" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray

$firewallLog = "C:\Windows\System32\LogFiles\Firewall\pfirewall.log"
if (Test-Path $firewallLog) {
    $recentDrops = Get-Content $firewallLog -Tail 100 | Select-String "DROP" | Select-String "21" -SimpleMatch | Select-Object -Last 10
    if ($recentDrops) {
        Write-Host "  ? Found firewall DROPS on port 21:" -ForegroundColor Yellow
        foreach ($drop in $recentDrops) {
            if ($drop -match $apiServerIP) {
                Write-Host "    $drop" -ForegroundColor Red
            } else {
                Write-Host "    $drop" -ForegroundColor Gray
            }
        }
    } else {
        Write-Host "  ? No recent drops on port 21 found in firewall log" -ForegroundColor Green
    }
} else {
    Write-Host "  ? Firewall logging not enabled" -ForegroundColor Yellow
    Write-Host "  ? Enable logging to troubleshoot dropped connections" -ForegroundColor Yellow
}
Write-Host ""

# ==============================================================================
# 14. RECOMMENDED ACTIONS
# ==============================================================================
Write-Host "???????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?  DIAGNOSTIC SUMMARY & RECOMMENDED ACTIONS                ?" -ForegroundColor Cyan
Write-Host "???????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

$issues = @()

if (-not $ftpService -or $ftpService.Status -ne 'Running') {
    $issues += "? FTP service is not running"
}

if (-not $listening) {
    $issues += "? Port 21 is not listening"
}

if (-not $ftpControlRule -or $ftpControlRule.Enabled -ne 'True') {
    $issues += "? FTP control port firewall rule is missing or disabled"
}

if (-not $ftpPassiveRule -or $ftpPassiveRule.Enabled -ne 'True') {
    $issues += "? FTP passive mode firewall rule is missing or disabled"
}

$activeFirewall = Get-NetFirewallProfile | Where-Object {$_.Enabled -eq 'True'}
if ($activeFirewall -and $ftpControlRule) {
    $ruleProfiles = $ftpControlRule.Profile -split ','
    $mismatch = $false
    foreach ($profile in $activeFirewall) {
        if ($ruleProfiles -notcontains $profile.Name) {
            $issues += "? Firewall rule may not apply to active profile: $($profile.Name)"
            $mismatch = $true
        }
    }
}

if ($issues.Count -eq 0) {
    Write-Host "  ? All checks passed!" -ForegroundColor Green
    Write-Host ""
    Write-Host "  If connection still fails, check:" -ForegroundColor Yellow
    Write-Host "    1. Router/Switch between servers" -ForegroundColor White
    Write-Host "    2. VLAN configuration" -ForegroundColor White
    Write-Host "    3. FTP client settings (passive mode enabled)" -ForegroundColor White
    Write-Host "    4. FTP server external IP configuration in IIS" -ForegroundColor White
} else {
    Write-Host "  Issues Found:" -ForegroundColor Red
    foreach ($issue in $issues) {
        Write-Host "    $issue" -ForegroundColor Yellow
    }
    Write-Host ""
    Write-Host "  ? RECOMMENDED ACTION:" -ForegroundColor Cyan
    Write-Host "    Re-run Fix-FtpServer-Firewall.ps1 to fix firewall rules" -ForegroundColor White
}

Write-Host ""
Write-Host "???????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "Diagnostics Complete" -ForegroundColor Green
Write-Host "???????????????????????????????????????????????????????????" -ForegroundColor Cyan
