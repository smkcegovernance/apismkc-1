# ?? Fix FTP Server Firewall (192.168.40.47)
# Run this script as Administrator on the FTP SERVER

Write-Host "??????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?  FTP Server Firewall Fix (192.168.40.47)                  ?" -ForegroundColor Cyan
Write-Host "??????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

$apiServerIP = "192.168.40.35"

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

# Step 1: Check FTP service
Write-Host "??????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "Step 1: Checking FTP Service" -ForegroundColor Cyan
Write-Host "??????????????????????????????????????????" -ForegroundColor DarkGray

$ftpService = Get-Service ftpsvc -ErrorAction SilentlyContinue
if ($ftpService) {
    Write-Host "  FTP Service: $($ftpService.Status)" -ForegroundColor $(if($ftpService.Status -eq 'Running'){'Green'}else{'Red'})
    
    if ($ftpService.Status -ne 'Running') {
        Write-Host "  Starting FTP service..." -ForegroundColor Yellow
        Start-Service ftpsvc
        Write-Host "  ? FTP service started" -ForegroundColor Green
    }
} else {
    Write-Host "  ? FTP service not found!" -ForegroundColor Red
    Write-Host "  Install FTP Server role in Server Manager" -ForegroundColor Yellow
    exit 1
}
Write-Host ""

# Step 2: Check what's listening on port 21
Write-Host "??????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "Step 2: Checking FTP Port 21" -ForegroundColor Cyan
Write-Host "??????????????????????????????????????????" -ForegroundColor DarkGray

$port21 = netstat -ano | findstr ":21 " | Select-String "LISTENING"
if ($port21) {
    Write-Host "  ? FTP listening on port 21" -ForegroundColor Green
    Write-Host "    $port21" -ForegroundColor Gray
} else {
    Write-Host "  ? Nothing listening on port 21!" -ForegroundColor Red
    Write-Host "  Check FTP site configuration in IIS Manager" -ForegroundColor Yellow
}
Write-Host ""

# Step 2.5: Check Active Firewall Profiles
Write-Host "??????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "Step 2.5: Checking Active Firewall Profiles" -ForegroundColor Cyan
Write-Host "??????????????????????????????????????????" -ForegroundColor DarkGray

$profiles = Get-NetFirewallProfile | Select-Object Name, Enabled, DefaultInboundAction
foreach ($profile in $profiles) {
    $status = if ($profile.Enabled) { "Enabled" } else { "Disabled" }
    $color = if ($profile.Enabled) { "Yellow" } else { "Gray" }
    Write-Host "  $($profile.Name): $status (Default Inbound: $($profile.DefaultInboundAction))" -ForegroundColor $color
}
Write-Host ""

# Step 3: Add inbound firewall rule
Write-Host "??????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "Step 3: Creating Inbound Firewall Rules" -ForegroundColor Cyan
Write-Host "??????????????????????????????????????????" -ForegroundColor DarkGray

try {
    # Remove existing rule if it exists
    $existingRule = Get-NetFirewallRule -DisplayName "FTP - Allow from SMKCAPI" -ErrorAction SilentlyContinue
    if ($existingRule) {
        Write-Host "  Removing existing rule..." -ForegroundColor Yellow
        Remove-NetFirewallRule -DisplayName "FTP - Allow from SMKCAPI" -ErrorAction SilentlyContinue
    }
    
    # Allow FTP control port from API server - APPLY TO ALL PROFILES
    New-NetFirewallRule -DisplayName "FTP - Allow from SMKCAPI" `
        -Description "Allow FTP connections from SMKCAPI server (192.168.40.35)" `
        -Direction Inbound `
        -Protocol TCP `
        -RemoteAddress $apiServerIP `
        -LocalPort 21 `
        -Action Allow `
        -Profile Domain,Private,Public `
        -Enabled True | Out-Null
    
    Write-Host "  ? Created firewall rule: FTP - Allow from SMKCAPI (All Profiles)" -ForegroundColor Green
    
    # Remove existing passive rule if it exists
    $existingPassive = Get-NetFirewallRule -DisplayName "FTP - Passive Mode Data" -ErrorAction SilentlyContinue
    if ($existingPassive) {
        Write-Host "  Removing existing passive rule..." -ForegroundColor Yellow
        Remove-NetFirewallRule -DisplayName "FTP - Passive Mode Data" -ErrorAction SilentlyContinue
    }
    
    # Allow passive mode data ports - APPLY TO ALL PROFILES
    New-NetFirewallRule -DisplayName "FTP - Passive Mode Data" `
        -Description "Allow FTP passive mode data connections from SMKCAPI" `
        -Direction Inbound `
        -Protocol TCP `
        -RemoteAddress $apiServerIP `
        -LocalPort 1024-65535 `
        -Action Allow `
        -Profile Domain,Private,Public `
        -Enabled True | Out-Null
    
    Write-Host "  ? Created firewall rule: FTP - Passive Mode Data (All Profiles)" -ForegroundColor Green
    
} catch {
    Write-Host "  ? Error creating firewall rules: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 4: Verify firewall rules with detailed information
Write-Host "??????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "Step 4: Verifying Firewall Rules" -ForegroundColor Cyan
Write-Host "??????????????????????????????????????????" -ForegroundColor DarkGray

$ftpControlRule = Get-NetFirewallRule -DisplayName "FTP - Allow from SMKCAPI" -ErrorAction SilentlyContinue
$ftpPassiveRule = Get-NetFirewallRule -DisplayName "FTP - Passive Mode Data" -ErrorAction SilentlyContinue

if ($ftpControlRule) {
    Write-Host "  ? FTP Control Rule (Port 21):" -ForegroundColor Green
    Write-Host "    - Enabled: $($ftpControlRule.Enabled)" -ForegroundColor White
    Write-Host "    - Direction: $($ftpControlRule.Direction)" -ForegroundColor White
    Write-Host "    - Action: $($ftpControlRule.Action)" -ForegroundColor White
    Write-Host "    - Profile: $($ftpControlRule.Profile)" -ForegroundColor White
    
    # Get port filter details
    $portFilter = $ftpControlRule | Get-NetFirewallPortFilter
    Write-Host "    - Local Port: $($portFilter.LocalPort)" -ForegroundColor White
    Write-Host "    - Protocol: $($portFilter.Protocol)" -ForegroundColor White
    
    # Get address filter details
    $addressFilter = $ftpControlRule | Get-NetFirewallAddressFilter
    Write-Host "    - Remote Address: $($addressFilter.RemoteAddress)" -ForegroundColor White
} else {
    Write-Host "  ??  FTP Control Rule NOT FOUND!" -ForegroundColor Red
}
Write-Host ""

if ($ftpPassiveRule) {
    Write-Host "  ? FTP Passive Rule (Ports 1024-65535):" -ForegroundColor Green
    Write-Host "    - Enabled: $($ftpPassiveRule.Enabled)" -ForegroundColor White
    Write-Host "    - Direction: $($ftpPassiveRule.Direction)" -ForegroundColor White
    Write-Host "    - Action: $($ftpPassiveRule.Action)" -ForegroundColor White
    Write-Host "    - Profile: $($ftpPassiveRule.Profile)" -ForegroundColor White
    
    # Get port filter details
    $portFilter = $ftpPassiveRule | Get-NetFirewallPortFilter
    Write-Host "    - Local Port: $($portFilter.LocalPort)" -ForegroundColor White
    
    # Get address filter details
    $addressFilter = $ftpPassiveRule | Get-NetFirewallAddressFilter
    Write-Host "    - Remote Address: $($addressFilter.RemoteAddress)" -ForegroundColor White
} else {
    Write-Host "  ??  FTP Passive Rule NOT FOUND!" -ForegroundColor Red
}
Write-Host ""

# Step 5: Test local connection
Write-Host "??????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "Step 5: Testing Local Connection" -ForegroundColor Cyan
Write-Host "??????????????????????????????????????????" -ForegroundColor DarkGray

try {
    $testResult = Test-NetConnection -ComputerName localhost -Port 21 -WarningAction SilentlyContinue
    if ($testResult.TcpTestSucceeded) {
        Write-Host "  ? Local FTP connection successful" -ForegroundColor Green
    } else {
        Write-Host "  ??  Local FTP connection failed" -ForegroundColor Yellow
        Write-Host "  This may indicate FTP service is not properly configured" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  ??  Could not test connection: $($_.Exception.Message)" -ForegroundColor Yellow
}
Write-Host ""

# Summary
Write-Host "??????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?  FTP SERVER CONFIGURATION COMPLETE                         ?" -ForegroundColor Cyan
Write-Host "??????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""
Write-Host "? Firewall rules created for ALL profiles (Domain, Private, Public)" -ForegroundColor Green
Write-Host "? FTP service verified" -ForegroundColor Green
Write-Host "? Rules allow connections from: $apiServerIP" -ForegroundColor Green
Write-Host ""
Write-Host "??????????????????????????????????????????" -ForegroundColor Yellow
Write-Host "NEXT STEP: Test from API server (192.168.40.35)" -ForegroundColor Yellow
Write-Host "??????????????????????????????????????????" -ForegroundColor Yellow
Write-Host ""
Write-Host "Run this command on 192.168.40.35:" -ForegroundColor White
Write-Host "  Test-NetConnection -ComputerName 192.168.40.47 -Port 21" -ForegroundColor Cyan
Write-Host ""
Write-Host "Expected result:" -ForegroundColor White
Write-Host "  TcpTestSucceeded : True" -ForegroundColor Green
Write-Host ""
