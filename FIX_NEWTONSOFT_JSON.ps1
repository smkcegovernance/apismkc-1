# Fix Newtonsoft.Json Version Mismatch
# Run this script after stopping the debugger

Write-Host "=== Fixing Newtonsoft.Json Version Mismatch ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Backup the .csproj file
Write-Host "Step 1: Backing up SmkcApi.csproj..." -ForegroundColor Yellow
Copy-Item "SmkcApi.csproj" "SmkcApi.csproj.backup" -Force
Write-Host "? Backup created: SmkcApi.csproj.backup" -ForegroundColor Green
Write-Host ""

# Step 2: Update the project file reference
Write-Host "Step 2: Updating project file reference..." -ForegroundColor Yellow
$csproj = Get-Content "SmkcApi.csproj" -Raw

# Replace old reference with new one
$oldRef = '<Reference Include="Newtonsoft.Json, Version=6.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed, processorArchitecture=MSIL">\s*<HintPath>packages\\Newtonsoft.Json.6.0.4\\lib\\net45\\Newtonsoft.Json.dll</HintPath>'
$newRef = '<Reference Include="Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed, processorArchitecture=MSIL"><HintPath>packages\Newtonsoft.Json.13.0.4\lib\net45\Newtonsoft.Json.dll</HintPath>'

if ($csproj -match $oldRef) {
    $csproj = $csproj -replace $oldRef, $newRef
    $csproj | Set-Content "SmkcApi.csproj" -NoNewline
    Write-Host "? Project file updated successfully" -ForegroundColor Green
} else {
    Write-Host "? Old reference not found, checking if already updated..." -ForegroundColor Yellow
    if ($csproj -match "Newtonsoft.Json.13.0.4") {
        Write-Host "? Project file already references version 13.0.4" -ForegroundColor Green
    } else {
        Write-Host "? Could not find expected reference pattern" -ForegroundColor Red
        Write-Host "Please manually update the reference in Visual Studio" -ForegroundColor Yellow
    }
}
Write-Host ""

# Step 3: Clean bin and obj directories
Write-Host "Step 3: Cleaning build directories..." -ForegroundColor Yellow
if (Test-Path "bin") {
    Remove-Item "bin" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "? bin directory cleaned" -ForegroundColor Green
}
if (Test-Path "obj") {
    Remove-Item "obj" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "? obj directory cleaned" -ForegroundColor Green
}
Write-Host ""

# Step 4: Remove old Newtonsoft.Json package folder
Write-Host "Step 4: Removing old Newtonsoft.Json 6.0.4 package..." -ForegroundColor Yellow
if (Test-Path "packages\Newtonsoft.Json.6.0.4") {
    Remove-Item "packages\Newtonsoft.Json.6.0.4" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "? Old package folder removed" -ForegroundColor Green
} else {
    Write-Host "? Old package folder not found (already removed)" -ForegroundColor Cyan
}
Write-Host ""

# Step 5: Verify packages
Write-Host "Step 5: Verifying packages..." -ForegroundColor Yellow
$packagesDir = Get-ChildItem "packages\Newtonsoft.Json*" -Directory
foreach ($pkg in $packagesDir) {
    $version = $pkg.Name -replace "Newtonsoft.Json.", ""
    Write-Host "  Found: Newtonsoft.Json version $version" -ForegroundColor Cyan
}
Write-Host ""

# Step 6: Check Web.config binding redirect
Write-Host "Step 6: Verifying Web.config binding redirect..." -ForegroundColor Yellow
$webConfig = Get-Content "Web.config" -Raw
if ($webConfig -match 'bindingRedirect oldVersion="0.0.0.0-13.0.0.0" newVersion="13.0.0.0"') {
    Write-Host "? Web.config binding redirect is correct (13.0.0.0)" -ForegroundColor Green
} elseif ($webConfig -match 'bindingRedirect oldVersion="0.0.0.0-6.0.0.0" newVersion="6.0.0.0"') {
    Write-Host "? Web.config still references version 6.0.0.0 - already fixed in previous step" -ForegroundColor Yellow
} else {
    Write-Host "? Web.config binding redirect format may be different" -ForegroundColor Cyan
}
Write-Host ""

Write-Host "=== Fix Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "1. Rebuild the solution in Visual Studio (Build ? Rebuild Solution)" -ForegroundColor White
Write-Host "2. Verify the DLL version with:" -ForegroundColor White
Write-Host "   [System.Reflection.Assembly]::LoadFile(`"$PWD\bin\Newtonsoft.Json.dll`").GetName().Version" -ForegroundColor Gray
Write-Host "3. Start debugging (F5)" -ForegroundColor White
Write-Host ""
Write-Host "Expected result: Version 13.0.0.0" -ForegroundColor Green
