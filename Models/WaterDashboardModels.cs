using System.Collections.Generic;

namespace SmkcApi.Models
{
    // ── Request ──────────────────────────────────────────────────────────────

    public class WaterDashboardRequest
    {
        public string FinYr    { get; set; } = "2026-2027";
        public string WardCode { get; set; } = "0"; // "0" = all
        public string DivCode  { get; set; } = "0"; // "0" = all
    }

    // ── Revenue Dashboard ────────────────────────────────────────────────────

    public class WaterRevenueDashboard
    {
        public string FinYr { get; set; }

        // Current billing cycle (latest BC in the finyr)
        public string  CurrentCycleDesc          { get; set; }
        public decimal CurrentCycleDemand        { get; set; }
        public decimal CurrentCyclePaid          { get; set; } // from WSCOLLECTIONTRANDET
        public decimal CurrentCycleBalance       { get; set; }
        public decimal CurrentCycleLateFees      { get; set; } // INT_CHARGED current BC/finyr
        public decimal CurrentCycleLateFeesBalance { get; set; }
        public decimal CurrentCycleMeterRent     { get; set; }

        // Current year arrear (older BC same finyr)
        public string  CurrYrArrearDesc          { get; set; }
        public decimal CurrYrArrearDemand        { get; set; }
        public decimal CurrYrArrearPaid          { get; set; }
        public decimal CurrYrArrearBalance       { get; set; }

        // Previous years aggregate balance + recovery
        public decimal PrevYearsArrearBalance    { get; set; }
        public decimal PrevYearsArrearCollected  { get; set; } // collected in this yr against prev yr demand
        public decimal TotalOutstanding          { get; set; }

        // Excess / advance credit held
        public decimal ExcessCreditBalance       { get; set; }

        // Aggregate for selected finyr
        public decimal TotalDemand               { get; set; }
        public decimal TotalCollected            { get; set; }
        public decimal CollectionEfficiencyPct   { get; set; }

        // Today's collection (current financial year receipts)
        public decimal TodayCollection          { get; set; }

        public List<PaymentMethodStat>    PaymentMethods   { get; set; } = new List<PaymentMethodStat>();
        public List<WardRevenueStat>      WardStats        { get; set; } = new List<WardRevenueStat>();
        public List<BillingCycleStat>     BillingCycles    { get; set; } = new List<BillingCycleStat>();
        public List<MonthlyCollectionStat> MonthlyTrend    { get; set; } = new List<MonthlyCollectionStat>();
        public List<DefaulterStat>        DefaultersByUsage { get; set; } = new List<DefaulterStat>();
        public List<YearlyRevenueTrend>   YearlyTrend      { get; set; } = new List<YearlyRevenueTrend>();
        public LastYearInsights           LastYearInsights { get; set; }
    }

    public class PaymentMethodStat
    {
        public string  Method   { get; set; }
        public int     Receipts { get; set; }
        public decimal Amount   { get; set; }
        public decimal PctShare { get; set; }
    }

    public class WardRevenueStat
    {
        public string  WardName       { get; set; }
        public int     Connections    { get; set; }
        public decimal Demand         { get; set; }
        public decimal Collected      { get; set; }
        public decimal Balance        { get; set; }
        public decimal EfficiencyPct  { get; set; }
    }

    public class BillingCycleStat
    {
        public string  CycleDesc      { get; set; }
        public int     BilledCount    { get; set; }
        public decimal Demand         { get; set; } // WS_CAMT + MRENT + INT_CHARGED
        public decimal Paid           { get; set; } // WS_COLLAMT + METERRENT + WS_PENALTY
        public decimal Balance        { get; set; } // WS_CBALAMT + INT_BALANCE
        public decimal MeterRent      { get; set; } // MRENT portion for breakdown display
        public decimal LateFees       { get; set; } // INT_CHARGED portion for breakdown display
        public bool    IsCurrentCycle { get; set; }
    }

    public class MonthlyCollectionStat
    {
        public string  MonthYr   { get; set; }
        public int     SortKey   { get; set; }
        public int     Receipts  { get; set; }
        public decimal Amount    { get; set; }
    }

    public class DefaulterStat
    {
        public string  UsageName    { get; set; }
        public int     PendingConns { get; set; }
        public decimal BalanceAmt   { get; set; }
        public decimal PctShare     { get; set; }
    }

    public class YearlyRevenueTrend
    {
        public string  FinYr             { get; set; }
        public decimal Demand            { get; set; }
        public decimal Collected         { get; set; } // from WSCOLLECTIONTRANDET
        public decimal Balance           { get; set; }
        public decimal EfficiencyPct     { get; set; }
        public int     BilledConnections { get; set; }
    }

    // ── Last Year (2025-26) at a Glance ────────────────────────────────────

    public class LastYearBcStat
    {
        public string  BcDesc        { get; set; }
        public int     BilledCount   { get; set; }
        public decimal Demand        { get; set; }
        public decimal Collected     { get; set; }
        public decimal Balance       { get; set; }
        public decimal EfficiencyPct { get; set; }
    }

    public class LastYearInsights
    {
        public string  FinYr                  { get; set; } = "2025-2026";
        // Demand raised (from wsdemanddet2526 snapshot)
        public decimal TotalDemand            { get; set; }
        public decimal WaterCharge            { get; set; }
        public decimal MeterRentBilled        { get; set; }
        public decimal LateFeesBilled         { get; set; }
        public int     ConnectionsBilled      { get; set; }
        // Collected during FY 2025-26
        public decimal TotalCollected         { get; set; }
        public decimal WaterChargeCollected   { get; set; }
        public decimal MeterRentCollected     { get; set; }
        public decimal LateFeesCollected      { get; set; }
        public decimal CollectionEfficiencyPct { get; set; }
        // Balance carried forward to 2026-27
        public decimal CarriedForward         { get; set; } // ws_cbalamt + bmrent + int_balance
        public int     ConnectionsCleared     { get; set; }
        public int     ConnectionsPending     { get; set; }
        public List<LastYearBcStat>    BillingCycles  { get; set; } = new List<LastYearBcStat>();
        public List<PaymentMethodStat> PaymentMethods { get; set; } = new List<PaymentMethodStat>();
    }

    // ── Connection Dashboard ────────────────────────────────────────────────

    public class WaterConnectionDashboard
    {
        // Connection status counts
        public int Total             { get; set; }
        public int ActiveConnected   { get; set; } // CONNFLAG=A AND DISCONNECTEDSTATUS NOT IN (P,T)
        public int NewPending        { get; set; } // CONNFLAG=N
        public int PermDisconnected  { get; set; } // DISCONNECTEDSTATUS=P
        public int TempDisconnected  { get; set; } // DISCONNECTEDSTATUS=T

        // Usage type counts
        public int Residential     { get; set; }
        public int NonResidential  { get; set; }
        public int Trust           { get; set; }
        public int Others          { get; set; }

        // Meter status
        public int Metered         { get; set; } // has meter number
        public int NonMetered      { get; set; }
        public int SmkcMeter       { get; set; }
        public int PrivateMeter    { get; set; }

        public List<MeterStatusStat>    MeterStatusBreakdown  { get; set; } = new List<MeterStatusStat>();
        public List<PipeSizeStat>       PipeSizeBreakdown     { get; set; } = new List<PipeSizeStat>();
        public List<WardConnectionStat> WardStats             { get; set; } = new List<WardConnectionStat>();
        public List<NewConnectionYear>  NewConnectionTrend    { get; set; } = new List<NewConnectionYear>();
        public List<UsageTypeConnStat>  UsageTypeBreakdown    { get; set; } = new List<UsageTypeConnStat>();
    }

    public class MeterStatusStat
    {
        public string MsDesc { get; set; }
        public int    Count  { get; set; }
        public decimal PctShare { get; set; }
    }

    public class PipeSizeStat
    {
        public string PipeDescMm    { get; set; }
        public string PipeDescInch  { get; set; }
        public int    Total         { get; set; }
        public int    Residential   { get; set; }
        public int    NonResidential { get; set; }
    }

    public class WardConnectionStat
    {
        public string WardName       { get; set; }
        public int    Total          { get; set; }
        public int    Residential    { get; set; }
        public int    NonResidential { get; set; }
        public int    Metered        { get; set; }
        public int    PermDisc       { get; set; }
    }

    public class NewConnectionYear
    {
        public string ConnYear      { get; set; }
        public int    Total         { get; set; }
        public int    Residential   { get; set; }
        public int    NonResidential { get; set; }
    }

    public class UsageTypeConnStat
    {
        public string UsageName { get; set; }
        public int    Count     { get; set; }
        public decimal PctShare { get; set; }
    }

    public class DivisionItem
    {
        public string DivCode { get; set; }
        public string DivName { get; set; }
    }
}
