# Verify Debug Configuration Script
# Checks if project is properly configured for debugging

Write-Host "=== Debug Configuration Verification ===" -ForegroundColor Cyan
Write-Host ""

$allChecks = @()

# Check 1: Project file debug settings
Write-Host "[1/8] Checking project debug configuration..." -ForegroundColor Yellow
$projContent = Get-Content "SmkcApi.csproj" -Raw
if ($projContent -match '<DebugType>full</DebugType>' -and $projContent -match '<Optimize>false</Optimize>') {
    Write-Host "      ? Project configured for full debug symbols" -ForegroundColor Green
    $allChecks += $true
} else {
    Write-Host "      ? Project not configured for debugging" -ForegroundColor Red
    $allChecks += $false
}

# Check 2: Web.config debug mode
Write-Host "[2/8] Checking Web.config debug mode..." -ForegroundColor Yellow
$webConfig = Get-Content "Web.config" -Raw
if ($webConfig -match 'compilation debug="true"') {
    Write-Host "      ? Web.config has debug=true" -ForegroundColor Green
    $allChecks += $true
} else {
    Write-Host "      ? Web.config has debug=false" -ForegroundColor Red
    Write-Host "        Change: <compilation debug='true' targetFramework='4.5' />" -ForegroundColor Yellow
    $allChecks += $false
}

# Check 3: Bin folder exists
Write-Host "[3/8] Checking bin folder..." -ForegroundColor Yellow
if (Test-Path "bin") {
    Write-Host "      ? Bin folder exists" -ForegroundColor Green
    $allChecks += $true
} else {
    Write-Host "      ? Bin folder missing" -ForegroundColor Red
    Write-Host "        Run: MSBuild apismkc.sln /t:Build /p:Configuration=Debug" -ForegroundColor Yellow
    $allChecks += $false
}

# Check 4: DLL exists
Write-Host "[4/8] Checking compiled DLL..." -ForegroundColor Yellow
if (Test-Path "bin\SmkcApi.dll") {
    $dll = Get-Item "bin\SmkcApi.dll"
    Write-Host "      ? SmkcApi.dll exists" -ForegroundColor Green
    Write-Host "        Last modified: $($dll.LastWriteTime)" -ForegroundColor Gray
    $allChecks += $true
} else {
    Write-Host "      ? SmkcApi.dll not found" -ForegroundColor Red
    $allChecks += $false
}

# Check 5: PDB file exists
Write-Host "[5/8] Checking debug symbols (PDB)..." -ForegroundColor Yellow
if (Test-Path "bin\SmkcApi.pdb") {
    $pdb = Get-Item "bin\SmkcApi.pdb"
    $dll = Get-Item "bin\SmkcApi.dll" -ErrorAction SilentlyContinue
    Write-Host "      ? SmkcApi.pdb exists" -ForegroundColor Green
    Write-Host "        Size: $($pdb.Length) bytes" -ForegroundColor Gray
    Write-Host "        Last modified: $($pdb.LastWriteTime)" -ForegroundColor Gray
    
    if ($dll -and $pdb.LastWriteTime -lt $dll.LastWriteTime.AddSeconds(-5)) {
        Write-Host "      ? Warning: PDB older than DLL" -ForegroundColor Yellow
        Write-Host "        Run: MSBuild apismkc.sln /t:Rebuild /p:Configuration=Debug" -ForegroundColor Yellow
    }
    $allChecks += $true
} else {
    Write-Host "      ? SmkcApi.pdb not found (required for debugging)" -ForegroundColor Red
    Write-Host "        Run: MSBuild apismkc.sln /t:Rebuild /p:Configuration=Debug /p:DebugSymbols=true /p:DebugType=full" -ForegroundColor Yellow
    $allChecks += $false
}

# Check 6: IIS Express settings
Write-Host "[6/8] Checking IIS Express configuration..." -ForegroundColor Yellow
if ($projContent -match 'UseIISExpress>true') {
    Write-Host "      ? Using IIS Express" -ForegroundColor Green
    if ($projContent -match 'IISUrl>([^<]+)</IISUrl>') {
        $iisUrl = $matches[1]
        Write-Host "        URL: $iisUrl" -ForegroundColor Gray
    }
    $allChecks += $true
} else {
    Write-Host "      ? Not using IIS Express" -ForegroundColor Gray
    $allChecks += $true
}

# Check 7: Route configuration
Write-Host "[7/8] Checking Web API configuration..." -ForegroundColor Yellow
if (Test-Path "App_Start\WebApiConfig.cs") {
    $webApiConfig = Get-Content "App_Start\WebApiConfig.cs" -Raw
    if ($webApiConfig -match 'MapHttpAttributeRoutes') {
        Write-Host "      ? Attribute routing enabled" -ForegroundColor Green
        $allChecks += $true
    } else {
        Write-Host "      ? Attribute routing not found" -ForegroundColor Red
        $allChecks += $false
    }
} else {
    Write-Host "      ? WebApiConfig.cs not found" -ForegroundColor Red
    $allChecks += $false
}

# Check 8: Controller file exists
Write-Host "[8/8] Checking ConsentDocumentController..." -ForegroundColor Yellow
$controllerPath = "Controllers\DepositManager\ConsentDocumentController.cs"
if (Test-Path $controllerPath) {
    $controller = Get-Content $controllerPath -Raw
    if ($controller -match '\[RoutePrefix\("api/deposits/consent"\)\]') {
        Write-Host "      ? Controller exists with correct route prefix" -ForegroundColor Green
        $allChecks += $true
    } else {
        Write-Host "      ? Controller exists but route prefix not found" -ForegroundColor Yellow
        $allChecks += $false
    }
} else {
    Write-Host "      ? ConsentDocumentController.cs not found" -ForegroundColor Red
    $allChecks += $false
}

# Summary
Write-Host ""
Write-Host "=== SUMMARY ===" -ForegroundColor Cyan
$passedChecks = ($allChecks | Where-Object { $_ -eq $true }).Count
$totalChecks = $allChecks.Count
$percentage = [math]::Round(($passedChecks / $totalChecks) * 100)

Write-Host "Passed: $passedChecks / $totalChecks ($percentage%)" -ForegroundColor $(if ($passedChecks -eq $totalChecks) { "Green" } elseif ($passedChecks -ge ($totalChecks * 0.75)) { "Yellow" } else { "Red" })
Write-Host ""

if ($passedChecks -eq $totalChecks) {
    Write-Host "? All checks passed!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor White
    Write-Host "1. Open Visual Studio" -ForegroundColor Gray
    Write-Host "2. Set Configuration to 'Debug'" -ForegroundColor Gray
    Write-Host "3. Set breakpoint in ConsentDocumentController" -ForegroundColor Gray
    Write-Host "4. Press F5 to start debugging" -ForegroundColor Gray
    Write-Host "5. Call: http://localhost:57031/api/deposits/consent/health" -ForegroundColor Gray
} elseif ($passedChecks -ge ($totalChecks * 0.75)) {
    Write-Host "? Most checks passed, but some issues found" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Recommended action:" -ForegroundColor White
    Write-Host "Run: .\Fix-Debug-Issues.ps1" -ForegroundColor Yellow
} else {
    Write-Host "? Multiple issues found" -ForegroundColor Red
    Write-Host ""
    Write-Host "Required action:" -ForegroundColor White
    Write-Host "Run: .\Fix-Debug-Issues.ps1" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== COMMON ISSUES ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Issue: Breakpoint is hollow circle" -ForegroundColor Yellow
Write-Host "  ? PDB file missing or outdated" -ForegroundColor Gray
Write-Host "  ? Solution: Run .\Fix-Debug-Issues.ps1" -ForegroundColor White
Write-Host ""
Write-Host "Issue: Breakpoint never hits" -ForegroundColor Yellow
Write-Host "  ? Using Ctrl+F5 instead of F5" -ForegroundColor Gray
Write-Host "  ? Configuration set to 'Release'" -ForegroundColor Gray
Write-Host "  ? Wrong URL or route" -ForegroundColor Gray
Write-Host "  ? Solution: Check configuration dropdown, use F5" -ForegroundColor White
Write-Host ""
Write-Host "Issue: Request returns 401 Unauthorized" -ForegroundColor Yellow
Write-Host "  ? Authentication filter blocking request" -ForegroundColor Gray
Write-Host "  ? Solution: Test /health endpoint first (has [AllowAnonymous])" -ForegroundColor White
Write-Host ""
Write-Host "Issue: Request returns 404 Not Found" -ForegroundColor Yellow
Write-Host "  ? Route not registered" -ForegroundColor Gray
Write-Host "  ? Wrong URL" -ForegroundColor Gray
Write-Host "  ? Solution: Verify attribute routing in WebApiConfig" -ForegroundColor White
Write-Host ""
