$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionPath = Join-Path $projectRoot 'apismkc.sln'
$msbuildPath = 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe'
$iisExpressPath = 'C:\Program Files (x86)\IIS Express\iisexpress.exe'
$port = 57031

if (-not (Test-Path $solutionPath)) {
    throw "Solution not found: $solutionPath"
}

if (-not (Test-Path $msbuildPath)) {
    throw "MSBuild not found: $msbuildPath"
}

if (-not (Test-Path $iisExpressPath)) {
    throw "IIS Express not found: $iisExpressPath"
}

Write-Host 'Stopping existing IIS Express processes...' -ForegroundColor Yellow
Get-Process -Name 'iisexpress' -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host 'Building apismkc.sln (Debug|Any CPU)...' -ForegroundColor Yellow
& $msbuildPath $solutionPath /t:Build /p:Configuration=Debug /p:Platform="Any CPU" /nologo /v:m
if ($LASTEXITCODE -ne 0) {
    throw 'Build failed.'
}

Write-Host "Starting IIS Express for $projectRoot on http://localhost:$port ..." -ForegroundColor Yellow
$iisProcess = Start-Process -FilePath $iisExpressPath -ArgumentList "/path:$projectRoot", "/port:$port" -PassThru

if (-not $iisProcess -or $iisProcess.HasExited) {
    throw 'IIS Express failed to start.'
}

Write-Host "IIS Express started successfully. PID: $($iisProcess.Id)" -ForegroundColor Green
Write-Host "URL: http://localhost:$port/" -ForegroundColor Cyan