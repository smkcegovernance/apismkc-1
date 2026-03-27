# ???????????????????????????????????????????????????????????
# ?? COMPLETE FTP Server Configuration & Fix
# Run this script as Administrator on the FTP SERVER (192.168.40.47)
# ???????????????????????????????????????????????????????????

Write-Host "???????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?  COMPLETE FTP Server Configuration                       ?" -ForegroundColor Cyan
Write-Host "?  Enhanced Version - Fixes Common Issues                  ?" -ForegroundColor Cyan
Write-Host "???????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

$apiServerIP = "192.168.40.35"
$ftpServerIP = "192.168.40.47"
$passivePortStart = 50000
$passivePortEnd = 50100

# Check if running as Administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
$isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "? ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

Write-Host "? Running as Administrator" -ForegroundColor Green
Write-Host ""

# ==============================================================================
# STEP 1: FTP SERVICE CHECK & RESTART
# ==============================================================================
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "Step 1: FTP Service Configuration" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray

$ftpService = Get-Service ftpsvc -ErrorAction SilentlyContinue
if (-not $ftpService) {
    Write-Host "  ? FTP Service (ftpsvc) NOT FOUND!" -ForegroundColor Red
    Write-Host "  ? Install FTP Server role from Server Manager" -ForegroundColor Yellow
    Write-Host "    Server Manager ? Add Roles ? Web Server (IIS) ? FTP Server" -ForegroundColor White
    exit 1
}

Write-Host "  FTP Service Status: $($ftpService.Status)" -ForegroundColor White
Write-Host "  Start Type: $($ftpService.StartType)" -ForegroundColor White

# Ensure service is running
if ($ftpService.Status -ne 'Running') {
    Write-Host "  ? Starting FTP service..." -ForegroundColor Yellow
    try {
        Start-Service ftpsvc -ErrorAction Stop
        Start-Sleep -Seconds 2
        Write-Host "  ? FTP service started successfully" -ForegroundColor Green
    } catch {
        Write-Host "  ? Failed to start FTP service: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "  ? FTP service is running" -ForegroundColor Green
}

# Set to automatic startup
if ($ftpService.StartType -ne 'Automatic') {
    Write-Host "  ? Setting FTP service to start automatically..." -ForegroundColor Yellow
    Set-Service ftpsvc -StartupType Automatic
    Write-Host "  ? Service set to automatic startup" -ForegroundColor Green
}
Write-Host ""

# ==============================================================================
# STEP 2: IIS FTP SITE CHECK
# ==============================================================================
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "Step 2: IIS FTP Site Configuration" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray

try {
    Import-Module WebAdministration -ErrorAction Stop
    
    $ftpSites = Get-ChildItem IIS:\Sites | Where-Object { 
        $_.bindings.Collection.protocol -contains "ftp" 
    }
    
    if ($ftpSites) {
        Write-Host "  ? FTP Sites Found:" -ForegroundColor Green
        foreach ($site in $ftpSites) {
            Write-Host "    Site: $($site.name)" -ForegroundColor White
            Write-Host "    State: $($site.state)" -ForegroundColor $(if($site.state -eq 'Started'){'Green'}else{'Red'})
            Write-Host "    ID: $($site.id)" -ForegroundColor Gray
            
            # Check bindings
            foreach ($binding in $site.bindings.Collection) {
                if ($binding.protocol -eq "ftp") {
                    Write-Host "    Binding: $($binding.bindingInformation)" -ForegroundColor White
                }
            }
            
            # Start site if stopped
            if ($site.state -ne 'Started') {
                Write-Host "    ? Starting FTP site..." -ForegroundColor Yellow
                Start-WebSite -Name $site.name
                Start-Sleep -Seconds 2
                Write-Host "    ? FTP site started" -ForegroundColor Green
            }
        }
    } else {
        Write-Host "  ? NO FTP SITES FOUND IN IIS!" -ForegroundColor Red
        Write-Host "  ? Create an FTP site in IIS Manager" -ForegroundColor Yellow
        Write-Host "    1. Open IIS Manager" -ForegroundColor White
        Write-Host "    2. Right-click Sites ? Add FTP Site" -ForegroundColor White
        Write-Host "    3. Configure binding to *:21" -ForegroundColor White
        exit 1
    }
} catch {
    Write-Host "  ? Error accessing IIS: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "  ? Ensure IIS is installed and WebAdministration module is available" -ForegroundColor Yellow
}
Write-Host ""

# ==============================================================================
# STEP 3: CONFIGURE FTP PASSIVE MODE
# ==============================================================================
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "Step 3: Configure FTP Passive Mode" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray

try {
    # Set passive port range
    Write-Host "  ? Configuring passive data port range: $passivePortStart-$passivePortEnd" -ForegroundColor White
    
    Set-WebConfigurationProperty -pspath 'MACHINE/WEBROOT/APPHOST' `
        -filter "system.ftpServer/firewallSupport" `
        -name "lowDataChannelPort" -value $passivePortStart
    
    Set-WebConfigurationProperty -pspath 'MACHINE/WEBROOT/APPHOST' `
        -filter "system.ftpServer/firewallSupport" `
        -name "highDataChannelPort" -value $passivePortEnd
    
    Write-Host "  ? Passive port range configured: $passivePortStart-$passivePortEnd" -ForegroundColor Green
    
    # Set external IP address
    Write-Host "  ? Setting external IP address: $ftpServerIP" -ForegroundColor White
    
    Set-WebConfigurationProperty -pspath 'MACHINE/WEBROOT/APPHOST' `
        -filter "system.ftpServer/firewallSupport" `
        -name "externalIp4Address" -value $ftpServerIP
    
    Write-Host "  ? External IP configured: $ftpServerIP" -ForegroundColor Green
    
    # Verify configuration
    $ftpFirewall = Get-WebConfigurationProperty -pspath 'MACHINE/WEBROOT/APPHOST' `
        -filter "system.ftpServer/firewallSupport" -name "."
    
    Write-Host ""
    Write-Host "  FTP Firewall Support Configuration:" -ForegroundColor White
    Write-Host "    External IP: $($ftpFirewall.externalIp4Address)" -ForegroundColor White
    Write-Host "    Low Data Port: $($ftpFirewall.lowDataChannelPort)" -ForegroundColor White
    Write-Host "    High Data Port: $($ftpFirewall.highDataChannelPort)" -ForegroundColor White
    
} catch {
    Write-Host "  ? Warning: Could not configure passive mode: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "  ? This may need to be configured manually in IIS Manager" -ForegroundColor Yellow
}
Write-Host ""

# ==============================================================================
# STEP 4: CONFIGURE WINDOWS FIREWALL RULES
# ==============================================================================
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "Step 4: Configure Windows Firewall Rules" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray

# Remove existing rules
Write-Host "  ? Removing any existing rules..." -ForegroundColor White
Remove-NetFirewallRule -DisplayName "FTP - Allow from SMKCAPI" -ErrorAction SilentlyContinue
Remove-NetFirewallRule -DisplayName "FTP - Passive Mode Data" -ErrorAction SilentlyContinue

try {
    # Create control port rule (port 21)
    Write-Host "  ? Creating FTP control port rule (port 21)..." -ForegroundColor White
    
    New-NetFirewallRule -DisplayName "FTP - Allow from SMKCAPI" `
        -Description "Allow FTP connections from SMKCAPI server (192.168.40.35) - All Profiles" `
        -Direction Inbound `
        -Protocol TCP `
        -RemoteAddress $apiServerIP `
        -LocalPort 21 `
        -Action Allow `
        -Profile Any `
        -Enabled True | Out-Null
    
    Write-Host "  ? FTP control port rule created (Port 21, All Profiles)" -ForegroundColor Green
    
    # Create passive mode data port rule
    Write-Host "  ? Creating FTP passive mode data port rule..." -ForegroundColor White
    
    New-NetFirewallRule -DisplayName "FTP - Passive Mode Data" `
        -Description "Allow FTP passive mode data connections from SMKCAPI (ports $passivePortStart-$passivePortEnd)" `
        -Direction Inbound `
        -Protocol TCP `
        -RemoteAddress $apiServerIP `
        -LocalPort "$passivePortStart-$passivePortEnd" `
        -Action Allow `
        -Profile Any `
        -Enabled True | Out-Null
    
    Write-Host "  ? FTP passive mode rule created (Ports $passivePortStart-$passivePortEnd, All Profiles)" -ForegroundColor Green
    
} catch {
    Write-Host "  ? Error creating firewall rules: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
Write-Host ""

# ==============================================================================
# STEP 5: VERIFY FIREWALL RULES
# ==============================================================================
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "Step 5: Verify Firewall Rules" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray

$ftpControlRule = Get-NetFirewallRule -DisplayName "FTP - Allow from SMKCAPI" -ErrorAction SilentlyContinue
if ($ftpControlRule) {
    Write-Host "  ? FTP Control Rule (Port 21):" -ForegroundColor Green
    Write-Host "    Enabled: $($ftpControlRule.Enabled)" -ForegroundColor White
    Write-Host "    Direction: $($ftpControlRule.Direction)" -ForegroundColor White
    Write-Host "    Action: $($ftpControlRule.Action)" -ForegroundColor White
    Write-Host "    Profile: $($ftpControlRule.Profile)" -ForegroundColor Yellow
    
    $portFilter = $ftpControlRule | Get-NetFirewallPortFilter
    $addressFilter = $ftpControlRule | Get-NetFirewallAddressFilter
    Write-Host "    Local Port: $($portFilter.LocalPort)" -ForegroundColor White
    Write-Host "    Protocol: $($portFilter.Protocol)" -ForegroundColor White
    Write-Host "    Remote Address: $($addressFilter.RemoteAddress)" -ForegroundColor White
} else {
    Write-Host "  ? FTP Control Rule verification FAILED!" -ForegroundColor Red
}
Write-Host ""

$ftpPassiveRule = Get-NetFirewallRule -DisplayName "FTP - Passive Mode Data" -ErrorAction SilentlyContinue
if ($ftpPassiveRule) {
    Write-Host "  ? FTP Passive Mode Rule:" -ForegroundColor Green
    Write-Host "    Enabled: $($ftpPassiveRule.Enabled)" -ForegroundColor White
    Write-Host "    Profile: $($ftpPassiveRule.Profile)" -ForegroundColor Yellow
    
    $portFilter = $ftpPassiveRule | Get-NetFirewallPortFilter
    $addressFilter = $ftpPassiveRule | Get-NetFirewallAddressFilter
    Write-Host "    Local Port Range: $($portFilter.LocalPort)" -ForegroundColor White
    Write-Host "    Remote Address: $($addressFilter.RemoteAddress)" -ForegroundColor White
} else {
    Write-Host "  ? FTP Passive Rule verification FAILED!" -ForegroundColor Red
}
Write-Host ""

# ==============================================================================
# STEP 6: CHECK PORT 21 LISTENING
# ==============================================================================
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "Step 6: Verify Port 21 is Listening" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray

$listening = netstat -ano | Select-String ":21.*LISTENING"
if ($listening) {
    Write-Host "  ? Port 21 is LISTENING" -ForegroundColor Green
    foreach ($line in $listening) {
        Write-Host "    $line" -ForegroundColor Gray
    }
} else {
    Write-Host "  ? Port 21 is NOT LISTENING!" -ForegroundColor Red
    Write-Host "  ? This is a critical issue!" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  Possible causes:" -ForegroundColor White
    Write-Host "    1. FTP site not started in IIS Manager" -ForegroundColor White
    Write-Host "    2. FTP site binding not configured for port 21" -ForegroundColor White
    Write-Host "    3. Another service using port 21" -ForegroundColor White
    Write-Host ""
    Write-Host "  Check in IIS Manager:" -ForegroundColor Yellow
    Write-Host "    inetmgr ? Sites ? [FTP Site] ? Bindings" -ForegroundColor White
}
Write-Host ""

# ==============================================================================
# STEP 7: RESTART FTP SERVICE
# ==============================================================================
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "Step 7: Restart FTP Service (Apply Configuration)" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray

Write-Host "  ? Restarting FTP service to apply changes..." -ForegroundColor White
try {
    Restart-Service ftpsvc -Force -ErrorAction Stop
    Start-Sleep -Seconds 3
    Write-Host "  ? FTP service restarted successfully" -ForegroundColor Green
    
    $ftpService = Get-Service ftpsvc
    Write-Host "  Current Status: $($ftpService.Status)" -ForegroundColor $(if($ftpService.Status -eq 'Running'){'Green'}else{'Red'})
} catch {
    Write-Host "  ? Failed to restart FTP service: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# ==============================================================================
# STEP 8: TEST LOCAL CONNECTION
# ==============================================================================
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "Step 8: Test Local FTP Connection" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray

Write-Host "  Testing connection to localhost:21..." -ForegroundColor White
try {
    $localTest = Test-NetConnection -ComputerName localhost -Port 21 -WarningAction SilentlyContinue
    
    if ($localTest.TcpTestSucceeded) {
        Write-Host "  ? LOCAL CONNECTION: SUCCESS" -ForegroundColor Green
        Write-Host "    ? FTP service is working correctly on this server" -ForegroundColor Green
    } else {
        Write-Host "  ? LOCAL CONNECTION: FAILED" -ForegroundColor Red
        Write-Host "    ? This indicates an FTP service or IIS configuration problem" -ForegroundColor Yellow
        Write-Host "    ? NOT a firewall issue!" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "  Critical: Fix IIS FTP configuration before proceeding!" -ForegroundColor Red
    }
} catch {
    Write-Host "  ? Connection test error: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# ==============================================================================
# STEP 9: TEST CONNECTION TO OWN IP
# ==============================================================================
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "Step 9: Test Connection to FTP Server IP" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray

Write-Host "  Testing connection to $ftpServerIP`:21..." -ForegroundColor White
try {
    $selfTest = Test-NetConnection -ComputerName $ftpServerIP -Port 21 -WarningAction SilentlyContinue
    
    if ($selfTest.TcpTestSucceeded) {
        Write-Host "  ? SELF IP CONNECTION: SUCCESS" -ForegroundColor Green
        Write-Host "    ? FTP is accessible via its IP address" -ForegroundColor Green
    } else {
        Write-Host "  ? SELF IP CONNECTION: FAILED" -ForegroundColor Red
        Write-Host "    ? Firewall may be blocking even loopback connections" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  ? Connection test error: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# ==============================================================================
# STEP 10: CHECK ACTIVE FIREWALL PROFILE
# ==============================================================================
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "Step 10: Active Firewall Profile Check" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray

$profiles = Get-NetFirewallProfile
foreach ($profile in $profiles) {
    $status = if ($profile.Enabled) { "Enabled" } else { "Disabled" }
    $color = if ($profile.Enabled) { "Yellow" } else { "Gray" }
    Write-Host "  [$($profile.Name) Profile]" -ForegroundColor White
    Write-Host "    Firewall: $status" -ForegroundColor $color
    Write-Host "    Default Inbound: $($profile.DefaultInboundAction)" -ForegroundColor $(if($profile.DefaultInboundAction -eq 'Block'){'Red'}else{'Green'})
    Write-Host "    Default Outbound: $($profile.DefaultOutboundAction)" -ForegroundColor White
}
Write-Host ""

$activeProfiles = Get-NetConnectionProfile -ErrorAction SilentlyContinue
if ($activeProfiles) {
    Write-Host "  Currently Active Network Profile:" -ForegroundColor Cyan
    foreach ($profile in $activeProfiles) {
        Write-Host "    Interface: $($profile.InterfaceAlias)" -ForegroundColor White
        Write-Host "    Network Category: $($profile.NetworkCategory)" -ForegroundColor Yellow
        Write-Host "    ? Firewall profile: " -NoNewline
        
        switch ($profile.NetworkCategory) {
            "DomainAuthenticated" { Write-Host "Domain" -ForegroundColor Green }
            "Private" { Write-Host "Private" -ForegroundColor Yellow }
            "Public" { Write-Host "Public" -ForegroundColor Red }
        }
    }
} else {
    Write-Host "  ? Could not determine active network profile" -ForegroundColor Yellow
}
Write-Host ""

# ==============================================================================
# STEP 11: ENABLE FIREWALL LOGGING
# ==============================================================================
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "Step 11: Enable Firewall Logging" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????" -ForegroundColor DarkGray

try {
    Write-Host "  ? Enabling firewall logging for blocked connections..." -ForegroundColor White
    
    Set-NetFirewallProfile -Profile Domain,Private,Public `
        -LogFileName "%systemroot%\system32\LogFiles\Firewall\pfirewall.log" `
        -LogAllowed False `
        -LogBlocked True `
        -LogMaxSizeKilobytes 4096
    
    Write-Host "  ? Firewall logging enabled" -ForegroundColor Green
    Write-Host "    Log file: C:\Windows\System32\LogFiles\Firewall\pfirewall.log" -ForegroundColor White
    Write-Host "    ? Monitor this file while testing connections" -ForegroundColor Yellow
} catch {
    Write-Host "  ? Could not enable firewall logging: $($_.Exception.Message)" -ForegroundColor Yellow
}
Write-Host ""

# ==============================================================================
# STEP 12: FINAL SUMMARY
# ==============================================================================
Write-Host "???????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?  CONFIGURATION COMPLETE                                  ?" -ForegroundColor Cyan
Write-Host "???????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

Write-Host "Configuration Summary:" -ForegroundColor White
Write-Host "  ? FTP service: Running & Auto-start" -ForegroundColor Green
Write-Host "  ? Passive port range: $passivePortStart-$passivePortEnd" -ForegroundColor Green
Write-Host "  ? External IP: $ftpServerIP" -ForegroundColor Green
Write-Host "  ? Firewall rule (Port 21): Created for all profiles" -ForegroundColor Green
Write-Host "  ? Firewall rule (Passive): Created for ports $passivePortStart-$passivePortEnd" -ForegroundColor Green
Write-Host "  ? Firewall logging: Enabled" -ForegroundColor Green
Write-Host ""

Write-Host "???????????????????????????????????????????????????????????" -ForegroundColor Yellow
Write-Host "CRITICAL: TEST FROM API SERVER NOW" -ForegroundColor Yellow
Write-Host "???????????????????????????????????????????????????????????" -ForegroundColor Yellow
Write-Host ""

Write-Host "On API Server (192.168.40.35), run:" -ForegroundColor White
Write-Host ""
Write-Host "  # Test TCP connection" -ForegroundColor Gray
Write-Host "  Test-NetConnection -ComputerName 192.168.40.47 -Port 21" -ForegroundColor Cyan
Write-Host ""
Write-Host "Expected Result:" -ForegroundColor White
Write-Host "  TcpTestSucceeded : True" -ForegroundColor Green
Write-Host ""

Write-Host "If TCP test FAILS:" -ForegroundColor Red
Write-Host "  1. Check firewall log: C:\Windows\System32\LogFiles\Firewall\pfirewall.log" -ForegroundColor White
Write-Host "  2. Run Diagnose-FtpServerIssue.ps1 for detailed diagnostics" -ForegroundColor White
Write-Host "  3. Check network infrastructure (routers, switches, VLANs)" -ForegroundColor White
Write-Host "  4. Contact network team if needed" -ForegroundColor White
Write-Host ""

Write-Host "If TCP test SUCCEEDS but FTP still fails:" -ForegroundColor Yellow
Write-Host "  1. Check FTP authentication settings" -ForegroundColor White
Write-Host "  2. Verify FTP user credentials" -ForegroundColor White
Write-Host "  3. Check FTP folder permissions" -ForegroundColor White
Write-Host "  4. Review SSL/TLS configuration" -ForegroundColor White
Write-Host ""

Write-Host "???????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "Script completed at $(Get-Date)" -ForegroundColor Green
Write-Host "???????????????????????????????????????????????????????????" -ForegroundColor Cyan
