# Debug Breakpoint Fix Script for SMKC API
# Run this script when debug breakpoints are not hitting

Write-Host "=== SMKC API Debug Fix Script ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Stop all IIS Express processes
Write-Host "[1/7] Stopping IIS Express processes..." -ForegroundColor Yellow
Get-Process -Name "iisexpress" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2
Write-Host "      ? IIS Express stopped" -ForegroundColor Green

# Step 2: Clear ASP.NET temporary files
Write-Host "[2/7] Clearing ASP.NET temporary files..." -ForegroundColor Yellow
$tempAspNetPath = "$env:LOCALAPPDATA\Temp\Temporary ASP.NET Files"
if (Test-Path $tempAspNetPath) {
    Remove-Item -Path "$tempAspNetPath\*" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "      ? Temporary files cleared" -ForegroundColor Green
} else {
    Write-Host "      ? No temporary files found" -ForegroundColor Gray
}

# Step 3: Delete bin and obj folders
Write-Host "[3/7] Deleting bin and obj folders..." -ForegroundColor Yellow
Remove-Item -Path "bin" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "obj" -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "      ? Build folders deleted" -ForegroundColor Green

# Step 4: Clear Visual Studio cache
Write-Host "[4/7] Clearing Visual Studio cache..." -ForegroundColor Yellow
Remove-Item -Path ".vs" -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "      ? VS cache cleared" -ForegroundColor Green

# Step 5: Clean solution
Write-Host "[5/7] Cleaning solution..." -ForegroundColor Yellow
$cleanResult = MSBuild apismkc.sln /t:Clean /p:Configuration=Debug /nologo /v:q
if ($LASTEXITCODE -eq 0) {
    Write-Host "      ? Solution cleaned" -ForegroundColor Green
} else {
    Write-Host "      ? Clean failed" -ForegroundColor Red
    exit 1
}

# Step 6: Rebuild with full debug symbols
Write-Host "[6/7] Rebuilding with debug symbols..." -ForegroundColor Yellow
$buildResult = MSBuild apismkc.sln /t:Build /p:Configuration=Debug /p:DebugSymbols=true /p:DebugType=full /nologo /v:q
if ($LASTEXITCODE -eq 0) {
    Write-Host "      ? Build successful" -ForegroundColor Green
} else {
    Write-Host "      ? Build failed" -ForegroundColor Red
    Write-Host ""
    Write-Host "Run this to see detailed errors:" -ForegroundColor Yellow
    Write-Host "MSBuild apismkc.sln /t:Build /p:Configuration=Debug /v:detailed" -ForegroundColor White
    exit 1
}

# Step 7: Verify PDB files exist
Write-Host "[7/7] Verifying debug symbols..." -ForegroundColor Yellow
$pdbFile = "bin\SmkcApi.pdb"
if (Test-Path $pdbFile) {
    $pdbInfo = Get-Item $pdbFile
    Write-Host "      ? Debug symbols generated" -ForegroundColor Green
    Write-Host "        Size: $($pdbInfo.Length) bytes" -ForegroundColor Gray
    Write-Host "        Date: $($pdbInfo.LastWriteTime)" -ForegroundColor Gray
} else {
    Write-Host "      ? Debug symbols not found" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== NEXT STEPS ===" -ForegroundColor Cyan
Write-Host "1. Close and reopen Visual Studio" -ForegroundColor White
Write-Host "2. Press F5 to start debugging" -ForegroundColor White
Write-Host "3. Test this endpoint first:" -ForegroundColor White
Write-Host "   GET http://localhost:57031/api/deposits/consent/health" -ForegroundColor Yellow
Write-Host ""
Write-Host "4. Set breakpoint in ConsentDocumentController.HealthCheck()" -ForegroundColor White
Write-Host "5. Verify breakpoint is solid (not hollow)" -ForegroundColor White
Write-Host ""
Write-Host "=== TROUBLESHOOTING ===" -ForegroundColor Cyan
Write-Host "If breakpoints still don't hit:" -ForegroundColor White
Write-Host "• Check toolbar shows 'Debug' (not 'Release')" -ForegroundColor Gray
Write-Host "• Verify you're using F5 (not Ctrl+F5)" -ForegroundColor Gray
Write-Host "• Check Output window (Ctrl+Alt+O) for errors" -ForegroundColor Gray
Write-Host "• Read DEBUG_BREAKPOINT_TROUBLESHOOTING.md for detailed help" -ForegroundColor Gray
Write-Host ""
Write-Host "? Script completed successfully!" -ForegroundColor Green
