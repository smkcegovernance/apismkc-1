Set-Location "c:\Users\ACER\source\repos\smkcegovernance\apismkc"

$oracleDll = Join-Path $PWD "bin\Oracle.ManagedDataAccess.dll"
$apiDll = Join-Path $PWD "bin\SmkcApi.dll"

Add-Type -Path $oracleDll
Add-Type -Path $apiDll

$code = @"
using Oracle.ManagedDataAccess.Client;
using SmkcApi.Repositories;

public class SmokeConnFactory : IOracleConnectionFactory
{
    private readonly string _cs;
    public SmokeConnFactory(string cs) { _cs = cs; }

    public OracleConnection Create() { return new OracleConnection(_cs); }
    public OracleConnection CreateWS() { return new OracleConnection(_cs); }
    public OracleConnection CreateUlberp() { return new OracleConnection(_cs); }
    public OracleConnection CreateAbas() { return new OracleConnection(_cs); }
    public OracleConnection CreateWebsite() { return new OracleConnection(_cs); }
}
"@

Add-Type -TypeDefinition $code -ReferencedAssemblies @("System.Data", $oracleDll, $apiDll)

$cs = "User Id=abas;Password=abas;Data Source=//SMKC-SCAN:1521/hcldb;Pooling=true;Min Pool Size=1;Max Pool Size=20;Connection Timeout=60;Validate Connection=true"
$connFactory = New-Object SmokeConnFactory($cs)
$repo = New-Object SmkcApi.Repositories.DepositManager.DepositRepository($connFactory)

function Show-Result {
    param(
        [string]$Name,
        [object]$Response
    )

    $rows = "n/a"
    if ($Response.Data -is [System.Data.DataTable]) {
        $rows = $Response.Data.Rows.Count
    }

    Write-Host ("{0} => Success={1}; Rows={2}; Message={3}" -f $Name, $Response.Success, $rows, $Response.Message)
}

Show-Result -Name "EnhancedKpis" -Response ($repo.Commissioner_GetEnhancedKpis($null, $null, $null, $null))
Show-Result -Name "BankWiseAnalytics" -Response ($repo.Commissioner_GetBankWiseAnalytics($null, $null, $null))
Show-Result -Name "UpcomingMaturities" -Response ($repo.Commissioner_GetUpcomingMaturities($null, $null, $null, $null, $null, $null))
Show-Result -Name "DepositTypeDistribution" -Response ($repo.Commissioner_GetDepositTypeDistribution($null, $null, $null))
Show-Result -Name "InterestTimeline" -Response ($repo.Commissioner_GetInterestTimeline($null, $null, $null, $null, $null))
Show-Result -Name "PortfolioHealth" -Response ($repo.Commissioner_GetPortfolioHealth($null, $null, $null, $null, $null, $null, $null))
