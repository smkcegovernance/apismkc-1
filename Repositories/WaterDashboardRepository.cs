using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using SmkcApi.Models;

namespace SmkcApi.Repositories
{
    public interface IWaterDashboardRepository
    {
        Task<WaterRevenueDashboard>    GetRevenueDashboardAsync(string finYr, string wardCode, string divCode);
        Task<WaterConnectionDashboard> GetConnectionDashboardAsync(string wardCode, string divCode);
        Task<List<DivisionItem>>       GetDivisionsAsync(string wardCode);
    }

    public class WaterDashboardRepository : IWaterDashboardRepository
    {
        private readonly IOracleConnectionFactory _factory;

        public WaterDashboardRepository(IOracleConnectionFactory factory)
        {
            _factory = factory;
        }

        // ─── Revenue Dashboard ────────────────────────────────────────────────

        public async Task<WaterRevenueDashboard> GetRevenueDashboardAsync(string finYr, string wardCode, string divCode)
        {
            var result = new WaterRevenueDashboard { FinYr = finYr };

            using (var conn = _factory.Create())
            {
                await conn.OpenAsync();

                // ── 1. Billing cycle demand breakdown (current cycle + current year arrear) ──
                await LoadBillingCyclesAsync(conn, result, finYr, wardCode, divCode);

                // ── 2. Payment method breakdown (collection year = finYr) ──
                await LoadPaymentMethodsAsync(conn, result, finYr, wardCode, divCode);

                // ── 3. Ward-wise demand / collection ──
                await LoadWardRevenueAsync(conn, result, finYr, wardCode, divCode);

                // ── 4. Monthly collection trend ──
                await LoadMonthlyTrendAsync(conn, result, finYr, wardCode, divCode);

                // ── 5. Defaulters by usage type ──
                await LoadDefaultersAsync(conn, result, finYr, wardCode, divCode);

                // ── 6. 8-year yearly trend ──
                await LoadYearlyTrendAsync(conn, result, wardCode, divCode);

                // ── 7. Excess credit balance ──
                await LoadExcessBalanceAsync(conn, result);

                // ── 8. Today's collection ──
                await LoadTodayCollectionAsync(conn, result, finYr);

                // ── 9. Previous year arrear recovery in this financial year ──
                await LoadPrevYrArrearsCollectedAsync(conn, result, finYr, wardCode, divCode);

                // ── 10. Last year (2025-26) at a glance from snapshot table ──
                await LoadLastYearInsightsAsync(conn, result);
            }

            // Compute aggregates
            // TotalDemand  = current year demand only (used for efficiency %)
            result.TotalDemand     = result.CurrentCycleDemand + result.CurrYrArrearDemand;
            // TotalCollected = collected against current year demand
            result.TotalCollected  = result.CurrentCyclePaid   + result.CurrYrArrearPaid;
            // TotalOutstanding = all unpaid balances (curr yr + prev yrs, including BMRENT+INT)
            result.TotalOutstanding = result.CurrentCycleBalance + result.CurrYrArrearBalance + result.PrevYearsArrearBalance;
            result.CollectionEfficiencyPct = result.TotalDemand > 0
                ? Math.Round(result.TotalCollected / result.TotalDemand * 100, 2)
                : 0;

            return result;
        }

        private async Task LoadBillingCyclesAsync(OracleConnection conn, WaterRevenueDashboard result, string finYr, string wardCode, string divCode)
        {
            string connFilter = WardDivExistsFilter(wardCode, divCode, "d.WS_CONNNO");

            string sql = $@"
                SELECT bc.BC_CODE, bc.BC_DESC,
                       COUNT(d.WS_CONNNO) AS BILLED_COUNT,
                       SUM(d.WS_CAMT + NVL(d.MRENT,0) + NVL(d.INT_CHARGED,0))  AS DEMAND,
                       SUM(d.WS_CBALAMT + NVL(d.INT_BALANCE,0))                 AS BALANCE,
                       SUM(d.INT_CHARGED)       AS LATE_FEES,
                       SUM(d.INT_BALANCE)       AS LATE_FEES_BAL,
                       SUM(d.MRENT)             AS METER_RENT
                  FROM WS.WSDEMANDDET d
                  JOIN WS.WSBILLINGCYCLEDET bc
                    ON d.BC_CODE = bc.BC_CODE AND d.ULB_CODE = bc.ULB_CODE
                 WHERE d.WS_FINYR = :finYr
                   {connFilter}
                 GROUP BY bc.BC_CODE, bc.BC_DESC
                 ORDER BY bc.BC_CODE DESC";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("finYr", finYr);
                AddWardDivParams(cmd, wardCode, divCode);

                var cycles   = new List<BillingCycleStat>();

                // Also get paid amounts from WSCOLLECTIONTRANDET
                var paidByBc = await GetPaidByBcAsync(conn, finYr, wardCode, divCode);

                using (var dr = await cmd.ExecuteReaderAsync())
                {
                    bool first = true;
                    while (await dr.ReadAsync())
                    {
                        int     bcCode    = Convert.ToInt32(dr["BC_CODE"]);
                        string  bcDesc    = dr["BC_DESC"]?.ToString();
                        int     cnt       = dr["BILLED_COUNT"] is DBNull ? 0 : Convert.ToInt32(dr["BILLED_COUNT"]);
                        decimal demand    = dr["DEMAND"]     is DBNull ? 0m : Convert.ToDecimal(dr["DEMAND"]);
                        decimal balance   = dr["BALANCE"]    is DBNull ? 0m : Convert.ToDecimal(dr["BALANCE"]);
                        decimal lateFees  = dr["LATE_FEES"]  is DBNull ? 0m : Convert.ToDecimal(dr["LATE_FEES"]);
                        decimal lfBal     = dr["LATE_FEES_BAL"] is DBNull ? 0m : Convert.ToDecimal(dr["LATE_FEES_BAL"]);
                        decimal mRent     = dr["METER_RENT"] is DBNull ? 0m : Convert.ToDecimal(dr["METER_RENT"]);

                        paidByBc.TryGetValue(bcCode, out decimal paid);

                        cycles.Add(new BillingCycleStat
                        {
                            CycleDesc     = bcDesc,
                            BilledCount   = cnt,
                            Demand        = demand,
                            Paid          = paid,
                            Balance       = balance,
                            MeterRent     = mRent,
                            LateFees      = lateFees,
                            IsCurrentCycle = first
                        });

                        if (first)
                        {
                            result.CurrentCycleDesc           = bcDesc;
                            result.CurrentCycleDemand         = demand;
                            result.CurrentCyclePaid           = paid;
                            result.CurrentCycleBalance        = balance;
                            result.CurrentCycleLateFees       = lateFees;
                            result.CurrentCycleLateFeesBalance = lfBal;
                            result.CurrentCycleMeterRent      = mRent;
                        }
                        else
                        {
                            result.CurrYrArrearDesc    = bcDesc;
                            result.CurrYrArrearDemand  = demand;
                            result.CurrYrArrearPaid    = paid;
                            result.CurrYrArrearBalance = balance;
                        }
                        first = false;
                    }
                }

                result.BillingCycles = cycles;
            }

            // Previous years balance (all finyr < current in WSDEMANDDET)
            // Uses BMRENT (meter-rent balance = billed minus collected) not MRENT (billed amount).
            // WS_COLFLAG='0' ensures only open/unpaid records are summed.
            string prevFilter = WardDivExistsFilter(wardCode, divCode, "d.WS_CONNNO");
            string prevSql = $@"
                SELECT NVL(SUM(d.WS_CBALAMT + NVL(d.BMRENT,0) + NVL(d.INT_BALANCE,0)), 0) AS PREV_BAL
                  FROM WS.WSDEMANDDET d
                 WHERE d.WS_FINYR < :finYr
                   AND d.WS_COLFLAG = '0'
                   {prevFilter}";

            using (var cmd2 = new OracleCommand(prevSql, conn))
            {
                cmd2.BindByName = true;
                cmd2.Parameters.Add("finYr", finYr);
                AddWardDivParams(cmd2, wardCode, divCode);

                var val = await cmd2.ExecuteScalarAsync();
                result.PrevYearsArrearBalance = val is DBNull ? 0m : Convert.ToDecimal(val);
            }
        }

        private async Task<Dictionary<int, decimal>> GetPaidByBcAsync(OracleConnection conn, string finYr, string wardCode, string divCode)
        {
            var dict = new Dictionary<int, decimal>();

            string connFilter = WardDivExistsFilter(wardCode, divCode, "t.WS_CONNNO");

            string sql = $@"
                SELECT t.BC_CODE, SUM(t.WS_COLLAMT + NVL(t.METERRENT,0) + NVL(t.WS_PENALTY,0)) AS PAID
                  FROM WS.WSCOLLECTIONTRANDET t
                 WHERE t.WS_FINYR = :finYr
                   {connFilter}
                 GROUP BY t.BC_CODE";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("finYr", finYr);
                AddWardDivParams(cmd, wardCode, divCode);

                using (var dr = await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        int bcCode = Convert.ToInt32(dr["BC_CODE"]);
                        decimal paid = dr["PAID"] is DBNull ? 0m : Convert.ToDecimal(dr["PAID"]);
                        dict[bcCode] = paid;
                    }
                }
            }
            return dict;
        }

        private async Task LoadPaymentMethodsAsync(OracleConnection conn, WaterRevenueDashboard result, string finYr, string wardCode, string divCode)
        {
            string connFilter = WardDivExistsFilter(wardCode, divCode, "c.WS_CONNNO");

            string sql = $@"
                SELECT ct.COLLTYPE_DESC,
                       COUNT(c.WS_RECEIPTNO)   AS RECEIPTS,
                       NVL(SUM(c.TOTAL_PAID),0) AS AMOUNT
                  FROM WS.WSCOLLECTIONDET_WB c
                  JOIN WS.WSCOLLECTIONTYPEDET ct
                    ON c.COLLTYPE_CODE = ct.COLLTYPE_CODE AND c.ULB_CODE = ct.ULB_CODE
                 WHERE c.CANCELLATIONFLAG = 'n'
                   AND c.STATUS = 'y'
                   AND c.WS_TRANFINYR = :finYr
                   {connFilter}
                 GROUP BY ct.COLLTYPE_CODE, ct.COLLTYPE_DESC
                 ORDER BY ct.COLLTYPE_CODE";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("finYr", finYr);
                AddWardDivParams(cmd, wardCode, divCode);

                var list     = new List<PaymentMethodStat>();
                decimal total = 0m;

                using (var dr = await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        var stat = new PaymentMethodStat
                        {
                            Method   = dr["COLLTYPE_DESC"]?.ToString(),
                            Receipts = dr["RECEIPTS"] is DBNull ? 0 : Convert.ToInt32(dr["RECEIPTS"]),
                            Amount   = dr["AMOUNT"]   is DBNull ? 0m : Convert.ToDecimal(dr["AMOUNT"]),
                        };
                        list.Add(stat);
                        total += stat.Amount;
                    }
                }

                // Compute pct shares
                foreach (var s in list)
                    s.PctShare = total > 0 ? Math.Round(s.Amount / total * 100, 1) : 0;

                result.PaymentMethods = list;
            }
        }

        private async Task LoadWardRevenueAsync(OracleConnection conn, WaterRevenueDashboard result, string finYr, string wardCode, string divCode)
        {
            string wardFilter = wardCode != "0" ? "AND c.WARD_CODE = :wardCode" : "";
            string divFilter  = divCode  != "0" ? "AND c.DIV_CODE  = :divCode"  : "";

            // No WS_FINYR filter: show total all-year outstanding per ward.
            // When no current-year billing exists (e.g. 2026-2027), all records are arrears
            // so this correctly reflects total outstanding demand and balance per ward.
            string sql = $@"
                SELECT w.WARD_NAME,
                       COUNT(DISTINCT c.WS_CONNNO)                              AS CONNECTIONS,
                       NVL(SUM(d.WS_CAMT + NVL(d.MRENT,0)),0)                  AS DEMAND,
                       NVL(SUM(d.WS_CHARGECOLAMT),0)                            AS COLLECTED,
                       NVL(SUM(d.WS_CBALAMT + NVL(d.BMRENT,0) + NVL(d.INT_BALANCE,0)),0) AS BALANCE
                  FROM WS.WSDEMANDDET d
                  JOIN WS.WSCONNECTIONDET c
                    ON d.WS_CONNNO = c.WS_CONNNO AND d.ULB_CODE = c.ULB_CODE
                  JOIN WS.WSWARDDET w
                    ON c.WARD_CODE = w.WARD_CODE AND c.ULB_CODE = w.ULB_CODE
                 WHERE d.WS_COLFLAG = '0'
                   {wardFilter}
                   {divFilter}
                 GROUP BY w.WARD_CODE, w.WARD_NAME
                 ORDER BY w.WARD_CODE";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                AddWardDivParams(cmd, wardCode, divCode);

                var list = new List<WardRevenueStat>();
                using (var dr = await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        decimal demand    = dr["DEMAND"]    is DBNull ? 0m : Convert.ToDecimal(dr["DEMAND"]);
                        decimal collected = dr["COLLECTED"] is DBNull ? 0m : Convert.ToDecimal(dr["COLLECTED"]);
                        list.Add(new WardRevenueStat
                        {
                            WardName      = dr["WARD_NAME"]?.ToString(),
                            Connections   = dr["CONNECTIONS"] is DBNull ? 0 : Convert.ToInt32(dr["CONNECTIONS"]),
                            Demand        = demand,
                            Collected     = collected,
                            Balance       = dr["BALANCE"] is DBNull ? 0m : Convert.ToDecimal(dr["BALANCE"]),
                            EfficiencyPct = demand > 0 ? Math.Round(collected / demand * 100, 1) : 0,
                        });
                    }
                }
                result.WardStats = list;
            }
        }

        private async Task LoadMonthlyTrendAsync(OracleConnection conn, WaterRevenueDashboard result, string finYr, string wardCode, string divCode)
        {
            string connFilter = WardDivExistsFilter(wardCode, divCode, "c.WS_CONNNO");

            string sql = $@"
                SELECT TO_CHAR(c.WS_RECEIPTDATE,'MON-YYYY') AS MONTH_YR,
                       EXTRACT(YEAR FROM c.WS_RECEIPTDATE)*100 + EXTRACT(MONTH FROM c.WS_RECEIPTDATE) AS SORT_KEY,
                       COUNT(c.WS_RECEIPTNO)    AS RECEIPTS,
                       NVL(SUM(c.TOTAL_PAID),0) AS AMOUNT
                  FROM WS.WSCOLLECTIONDET_WB c
                 WHERE c.CANCELLATIONFLAG = 'n'
                   AND c.STATUS = 'y'
                   AND c.WS_TRANFINYR = :finYr
                   {connFilter}
                 GROUP BY TO_CHAR(c.WS_RECEIPTDATE,'MON-YYYY'),
                          EXTRACT(YEAR FROM c.WS_RECEIPTDATE)*100 + EXTRACT(MONTH FROM c.WS_RECEIPTDATE)
                 ORDER BY SORT_KEY";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("finYr", finYr);
                AddWardDivParams(cmd, wardCode, divCode);

                var list = new List<MonthlyCollectionStat>();
                using (var dr = await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        int sk = dr["SORT_KEY"] is DBNull ? 0 : Convert.ToInt32(dr["SORT_KEY"]);
                        // Only include months within the financial year range
                        list.Add(new MonthlyCollectionStat
                        {
                            MonthYr  = dr["MONTH_YR"]?.ToString(),
                            SortKey  = sk,
                            Receipts = dr["RECEIPTS"] is DBNull ? 0 : Convert.ToInt32(dr["RECEIPTS"]),
                            Amount   = dr["AMOUNT"]   is DBNull ? 0m : Convert.ToDecimal(dr["AMOUNT"]),
                        });
                    }
                }
                result.MonthlyTrend = list;
            }
        }

        private async Task LoadDefaultersAsync(OracleConnection conn, WaterRevenueDashboard result, string finYr, string wardCode, string divCode)
        {
            string wardFilter = wardCode != "0" ? "AND c.WARD_CODE = :wardCode" : "";
            string divFilter  = divCode  != "0" ? "AND c.DIV_CODE  = :divCode"  : "";

            // No WS_FINYR filter: show total pending balance across all years per usage type.
            // For 2026-2027 (no current billing), this shows the full arrear defaulter list.
            string sql = $@"
                SELECT ut.USAGE_NAME,
                       COUNT(DISTINCT d.WS_CONNNO)                      AS PENDING_CONNS,
                       NVL(SUM(d.WS_CBALAMT + NVL(d.INT_BALANCE,0)),0) AS BALANCE_AMT
                  FROM WS.WSDEMANDDET d
                  JOIN WS.WSCONNECTIONDET c
                    ON d.WS_CONNNO = c.WS_CONNNO AND d.ULB_CODE = c.ULB_CODE
                  JOIN WS.WSUSAGETYPEDET ut
                    ON c.USAGE_CODE = ut.USAGE_CODE AND c.ULB_CODE = ut.ULB_CODE
                 WHERE d.WS_COLFLAG = '0'
                   {wardFilter}
                   {divFilter}
                 GROUP BY ut.USAGE_CODE, ut.USAGE_NAME
                 ORDER BY BALANCE_AMT DESC";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                AddWardDivParams(cmd, wardCode, divCode);

                var list = new List<DefaulterStat>();
                decimal total = 0m;
                using (var dr = await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        decimal bal = dr["BALANCE_AMT"] is DBNull ? 0m : Convert.ToDecimal(dr["BALANCE_AMT"]);
                        list.Add(new DefaulterStat
                        {
                            UsageName    = dr["USAGE_NAME"]?.ToString(),
                            PendingConns = dr["PENDING_CONNS"] is DBNull ? 0 : Convert.ToInt32(dr["PENDING_CONNS"]),
                            BalanceAmt   = bal,
                        });
                        total += bal;
                    }
                }
                foreach (var d in list)
                    d.PctShare = total > 0 ? Math.Round(d.BalanceAmt / total * 100, 1) : 0;

                result.DefaultersByUsage = list;
            }
        }

        private async Task LoadYearlyTrendAsync(OracleConnection conn, WaterRevenueDashboard result, string wardCode, string divCode)
        {
            string connFilter     = WardDivExistsFilter(wardCode, divCode, "d.WS_CONNNO");
            string connFilterPaid = WardDivExistsFilter(wardCode, divCode, "t.WS_CONNNO");

            // Demand from live WSDEMANDDET (includes MRENT + INT_CHARGED)
            string sql = $@"
                SELECT d.WS_FINYR,
                       NVL(SUM(d.WS_CAMT + NVL(d.MRENT,0) + NVL(d.INT_CHARGED,0)),0) AS DEMAND,
                       NVL(SUM(d.WS_CBALAMT + NVL(d.INT_BALANCE,0)),0)                AS BALANCE,
                       COUNT(DISTINCT d.WS_CONNNO)    AS BILLED_CONNS
                  FROM WS.WSDEMANDDET d
                 WHERE d.WS_FINYR >= '2018-2019'
                   {connFilter}
                 GROUP BY d.WS_FINYR
                 ORDER BY d.WS_FINYR";

            // Paid from WSCOLLECTIONTRANDET (includes METERRENT + WS_PENALTY)
            string sqlPaid = $@"
                SELECT t.WS_FINYR, NVL(SUM(t.WS_COLLAMT + NVL(t.METERRENT,0) + NVL(t.WS_PENALTY,0)),0) AS PAID
                  FROM WS.WSCOLLECTIONTRANDET t
                 WHERE t.WS_FINYR >= '2018-2019'
                   {connFilterPaid}
                 GROUP BY t.WS_FINYR";

            var paidMap = new Dictionary<string, decimal>();
            using (var cmd2 = new OracleCommand(sqlPaid, conn))
            {
                cmd2.BindByName = true;
                AddWardDivParams(cmd2, wardCode, divCode);
                using (var dr2 = await cmd2.ExecuteReaderAsync())
                {
                    while (await dr2.ReadAsync())
                    {
                        string yr = dr2["WS_FINYR"]?.ToString();
                        decimal p = dr2["PAID"] is DBNull ? 0m : Convert.ToDecimal(dr2["PAID"]);
                        if (yr != null) paidMap[yr] = p;
                    }
                }
            }

            var list = new List<YearlyRevenueTrend>();
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                AddWardDivParams(cmd, wardCode, divCode);

                using (var dr = await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        string yr     = dr["WS_FINYR"]?.ToString();
                        decimal dmnd  = dr["DEMAND"]  is DBNull ? 0m : Convert.ToDecimal(dr["DEMAND"]);
                        decimal bal   = dr["BALANCE"] is DBNull ? 0m : Convert.ToDecimal(dr["BALANCE"]);
                        int     conns = dr["BILLED_CONNS"] is DBNull ? 0 : Convert.ToInt32(dr["BILLED_CONNS"]);
                        paidMap.TryGetValue(yr, out decimal paid);
                        list.Add(new YearlyRevenueTrend
                        {
                            FinYr             = yr,
                            Demand            = dmnd,
                            Collected         = paid,
                            Balance           = bal,
                            EfficiencyPct     = dmnd > 0 ? Math.Round(paid / dmnd * 100, 1) : 0,
                            BilledConnections = conns,
                        });
                    }
                }
            }
            result.YearlyTrend = list;
        }

        private async Task LoadExcessBalanceAsync(OracleConnection conn, WaterRevenueDashboard result)
        {
            const string sql = "SELECT NVL(SUM(EXCESS_BAL),0) FROM WS.WSEXCESS_WB";
            using (var cmd = new OracleCommand(sql, conn))
            {
                var val = await cmd.ExecuteScalarAsync();
                result.ExcessCreditBalance = val is DBNull ? 0m : Convert.ToDecimal(val);
            }
        }

        private async Task LoadPrevYrArrearsCollectedAsync(OracleConnection conn, WaterRevenueDashboard result, string finYr, string wardCode, string divCode)
        {
            // Collections received in finYr (via WSCOLLECTIONDET_WB) but linked in WSCOLLECTIONTRANDET
            // to a demand year BEFORE finYr (i.e., recovery of previous year arrears)
            string connFilter = WardDivExistsFilter(wardCode, divCode, "t.WS_CONNNO");

            string sql = $@"
                SELECT NVL(SUM(t.WS_COLLAMT + NVL(t.METERRENT,0) + NVL(t.WS_PENALTY,0)), 0) AS PREV_COLLECTED
                  FROM WS.WSCOLLECTIONTRANDET t
                  JOIN WS.WSCOLLECTIONDET_WB c
                    ON t.WS_CONNNO = c.WS_CONNNO AND t.WS_RECEIPTNO = c.WS_RECEIPTNO
                 WHERE c.WS_TRANFINYR = :finYr
                   AND c.CANCELLATIONFLAG = 'n'
                   AND c.STATUS = 'y'
                   AND t.WS_FINYR < :finYr
                   {connFilter}";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("finYr", finYr);
                AddWardDivParams(cmd, wardCode, divCode);
                var val = await cmd.ExecuteScalarAsync();
                result.PrevYearsArrearCollected = val is DBNull ? 0m : Convert.ToDecimal(val);
            }
        }

        private async Task LoadTodayCollectionAsync(OracleConnection conn, WaterRevenueDashboard result, string finYr)
        {
            const string sql = @"
                SELECT NVL(SUM(c.TOTAL_PAID),0) AS TODAY_AMT
                  FROM WS.WSCOLLECTIONDET_WB c
                 WHERE c.CANCELLATIONFLAG = 'n'
                   AND c.STATUS = 'y'
                   AND c.WS_TRANFINYR = :finYr
                   AND TRUNC(c.WS_RECEIPTDATE) = TRUNC(SYSDATE)";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("finYr", finYr);
                var val = await cmd.ExecuteScalarAsync();
                result.TodayCollection = val is DBNull ? 0m : Convert.ToDecimal(val);
            }
        }

        // ─── Connection Dashboard ─────────────────────────────────────────────

        public async Task<WaterConnectionDashboard> GetConnectionDashboardAsync(string wardCode, string divCode)
        {
            var result = new WaterConnectionDashboard();

            using (var conn = _factory.Create())
            {
                await conn.OpenAsync();

                await LoadConnectionCountsAsync(conn, result, wardCode, divCode);
                await LoadPipeSizeAsync(conn, result, wardCode, divCode);
                await LoadWardConnectionsAsync(conn, result, wardCode, divCode);
                await LoadNewConnectionTrendAsync(conn, result, wardCode, divCode);
                await LoadUsageTypeBreakdownAsync(conn, result, wardCode, divCode);
                await LoadMeterStatusAsync(conn, result, wardCode, divCode);
            }

            return result;
        }

        private async Task LoadConnectionCountsAsync(OracleConnection conn, WaterConnectionDashboard result, string wardCode, string divCode)
        {
            string wardCond = wardCode != "0" ? "AND WARD_CODE = :wardCode" : "";
            string divCond  = divCode  != "0" ? "AND DIV_CODE  = :divCode"  : "";

            string sql = $@"
                SELECT
                  COUNT(*)                                                              AS TOTAL,
                  SUM(CASE WHEN CONNFLAG='A' AND DISCONNECTEDSTATUS NOT IN ('P','T') THEN 1 ELSE 0 END) AS ACTIVE_CONN,
                  SUM(CASE WHEN CONNFLAG='N' THEN 1 ELSE 0 END)                       AS NEW_PENDING,
                  SUM(CASE WHEN DISCONNECTEDSTATUS='P' THEN 1 ELSE 0 END)             AS PERM_DISC,
                  SUM(CASE WHEN DISCONNECTEDSTATUS='T' THEN 1 ELSE 0 END)             AS TEMP_DISC,
                  SUM(CASE WHEN USAGE_CODE=1 THEN 1 ELSE 0 END)                       AS RESIDENTIAL,
                  SUM(CASE WHEN USAGE_CODE=2 THEN 1 ELSE 0 END)                       AS NON_RESI,
                  SUM(CASE WHEN USAGE_CODE=3 THEN 1 ELSE 0 END)                       AS TRUST,
                  SUM(CASE WHEN USAGE_CODE NOT IN (1,2,3) THEN 1 ELSE 0 END)          AS OTHERS,
                  SUM(CASE WHEN LENGTH(TRIM(METER))>0 THEN 1 ELSE 0 END)              AS METERED,
                  SUM(CASE WHEN LENGTH(TRIM(METER)) IS NULL OR LENGTH(TRIM(METER))=0 THEN 1 ELSE 0 END) AS NON_METERED,
                  SUM(CASE WHEN METER_OTYPE='1' AND LENGTH(TRIM(METER))>0 THEN 1 ELSE 0 END) AS SMKC_METER,
                  SUM(CASE WHEN METER_OTYPE='2' AND LENGTH(TRIM(METER))>0 THEN 1 ELSE 0 END) AS PRIV_METER
                FROM WS.WSCONNECTIONDET
                WHERE 1=1 {wardCond} {divCond}";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                AddWardDivParams(cmd, wardCode, divCode);

                using (var dr = await cmd.ExecuteReaderAsync())
                {
                    if (await dr.ReadAsync())
                    {
                        result.Total            = ToInt(dr["TOTAL"]);
                        result.ActiveConnected  = ToInt(dr["ACTIVE_CONN"]);
                        result.NewPending       = ToInt(dr["NEW_PENDING"]);
                        result.PermDisconnected = ToInt(dr["PERM_DISC"]);
                        result.TempDisconnected = ToInt(dr["TEMP_DISC"]);
                        result.Residential      = ToInt(dr["RESIDENTIAL"]);
                        result.NonResidential   = ToInt(dr["NON_RESI"]);
                        result.Trust            = ToInt(dr["TRUST"]);
                        result.Others           = ToInt(dr["OTHERS"]);
                        result.Metered          = ToInt(dr["METERED"]);
                        result.NonMetered       = ToInt(dr["NON_METERED"]);
                        result.SmkcMeter        = ToInt(dr["SMKC_METER"]);
                        result.PrivateMeter     = ToInt(dr["PRIV_METER"]);
                    }
                }
            }
        }

        private async Task LoadPipeSizeAsync(OracleConnection conn, WaterConnectionDashboard result, string wardCode, string divCode)
        {
            string wardFilter = wardCode != "0" ? "AND c.WARD_CODE = :wardCode" : "";
            string divFilter  = divCode  != "0" ? "AND c.DIV_CODE  = :divCode"  : "";

            string sql = $@"
                SELECT p.PIPE_CODE, p.PIPE_DESCMM, p.PIPE_DESCINCH,
                       COUNT(*)                                                  AS TOTAL,
                       SUM(CASE WHEN c.USAGE_CODE=1 THEN 1 ELSE 0 END)          AS RESIDENTIAL,
                       SUM(CASE WHEN c.USAGE_CODE<>1 THEN 1 ELSE 0 END)         AS NON_RESI
                  FROM WS.WSCONNECTIONDET c
                  JOIN WS.WSPIPETYPEDET p ON c.PIPE_CODE = p.PIPE_CODE AND c.ULB_CODE = p.ULB_CODE
                 WHERE 1=1 {wardFilter} {divFilter}
                 GROUP BY p.PIPE_CODE, p.PIPE_DESCMM, p.PIPE_DESCINCH
                 ORDER BY p.PIPE_CODE";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                AddWardDivParams(cmd, wardCode, divCode);

                var list = new List<PipeSizeStat>();
                using (var dr = await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        list.Add(new PipeSizeStat
                        {
                            PipeDescMm     = dr["PIPE_DESCMM"]?.ToString(),
                            PipeDescInch   = dr["PIPE_DESCINCH"]?.ToString(),
                            Total          = ToInt(dr["TOTAL"]),
                            Residential    = ToInt(dr["RESIDENTIAL"]),
                            NonResidential = ToInt(dr["NON_RESI"]),
                        });
                    }
                }
                result.PipeSizeBreakdown = list;
            }
        }

        private async Task LoadWardConnectionsAsync(OracleConnection conn, WaterConnectionDashboard result, string wardCode, string divCode)
        {
            string wardFilter = wardCode != "0" ? "AND c.WARD_CODE = :wardCode" : "";
            string divFilter  = divCode  != "0" ? "AND c.DIV_CODE  = :divCode"  : "";

            string sql = $@"
                SELECT w.WARD_NAME,
                       COUNT(*)                                                  AS TOTAL,
                       SUM(CASE WHEN c.USAGE_CODE=1 THEN 1 ELSE 0 END)          AS RESIDENTIAL,
                       SUM(CASE WHEN c.USAGE_CODE<>1 THEN 1 ELSE 0 END)         AS NON_RESI,
                       SUM(CASE WHEN LENGTH(TRIM(c.METER))>0 THEN 1 ELSE 0 END) AS METERED,
                       SUM(CASE WHEN c.DISCONNECTEDSTATUS='P' THEN 1 ELSE 0 END) AS PERM_DISC
                  FROM WS.WSCONNECTIONDET c
                  JOIN WS.WSWARDDET w ON c.WARD_CODE = w.WARD_CODE AND c.ULB_CODE = w.ULB_CODE
                 WHERE 1=1 {wardFilter} {divFilter}
                 GROUP BY w.WARD_CODE, w.WARD_NAME
                 ORDER BY w.WARD_CODE";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                AddWardDivParams(cmd, wardCode, divCode);

                var list = new List<WardConnectionStat>();
                using (var dr = await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        list.Add(new WardConnectionStat
                        {
                            WardName       = dr["WARD_NAME"]?.ToString(),
                            Total          = ToInt(dr["TOTAL"]),
                            Residential    = ToInt(dr["RESIDENTIAL"]),
                            NonResidential = ToInt(dr["NON_RESI"]),
                            Metered        = ToInt(dr["METERED"]),
                            PermDisc       = ToInt(dr["PERM_DISC"]),
                        });
                    }
                }
                result.WardStats = list;
            }
        }

        private async Task LoadNewConnectionTrendAsync(OracleConnection conn, WaterConnectionDashboard result, string wardCode, string divCode)
        {
            string wardCond = wardCode != "0" ? "AND WARD_CODE = :wardCode" : "";
            string divCond  = divCode  != "0" ? "AND DIV_CODE  = :divCode"  : "";

            string sql = $@"
                SELECT TO_CHAR(WS_CONNDATE,'YYYY') AS CONN_YEAR,
                       COUNT(*)                                                  AS TOTAL,
                       SUM(CASE WHEN USAGE_CODE=1 THEN 1 ELSE 0 END)            AS RESIDENTIAL,
                       SUM(CASE WHEN USAGE_CODE<>1 THEN 1 ELSE 0 END)           AS NON_RESI
                  FROM WS.WSCONNECTIONDET
                 WHERE WS_CONNDATE IS NOT NULL {wardCond} {divCond}
                 GROUP BY TO_CHAR(WS_CONNDATE,'YYYY')
                 ORDER BY CONN_YEAR DESC
                 FETCH FIRST 10 ROWS ONLY";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                AddWardDivParams(cmd, wardCode, divCode);

                var list = new List<NewConnectionYear>();
                using (var dr = await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        list.Add(new NewConnectionYear
                        {
                            ConnYear       = dr["CONN_YEAR"]?.ToString(),
                            Total          = ToInt(dr["TOTAL"]),
                            Residential    = ToInt(dr["RESIDENTIAL"]),
                            NonResidential = ToInt(dr["NON_RESI"]),
                        });
                    }
                }
                result.NewConnectionTrend = list;
            }
        }

        private async Task LoadUsageTypeBreakdownAsync(OracleConnection conn, WaterConnectionDashboard result, string wardCode, string divCode)
        {
            string wardFilter = wardCode != "0" ? "AND c.WARD_CODE = :wardCode" : "";
            string divFilter  = divCode  != "0" ? "AND c.DIV_CODE  = :divCode"  : "";

            string sql = $@"
                SELECT ut.USAGE_NAME, COUNT(*) AS CNT
                  FROM WS.WSCONNECTIONDET c
                  JOIN WS.WSUSAGETYPEDET ut ON c.USAGE_CODE = ut.USAGE_CODE AND c.ULB_CODE = ut.ULB_CODE
                 WHERE 1=1 {wardFilter} {divFilter}
                 GROUP BY ut.USAGE_CODE, ut.USAGE_NAME
                 ORDER BY CNT DESC";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                AddWardDivParams(cmd, wardCode, divCode);

                var list = new List<UsageTypeConnStat>();
                int total = result.Total > 0 ? result.Total : 1;
                using (var dr = await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        int cnt = ToInt(dr["CNT"]);
                        list.Add(new UsageTypeConnStat
                        {
                            UsageName = dr["USAGE_NAME"]?.ToString(),
                            Count     = cnt,
                            PctShare  = Math.Round((decimal)cnt / total * 100, 1),
                        });
                    }
                }
                result.UsageTypeBreakdown = list;
            }
        }

        private async Task LoadMeterStatusAsync(OracleConnection conn, WaterConnectionDashboard result, string wardCode, string divCode)
        {
            string wardCond = wardCode != "0" ? "AND c.WARD_CODE = :wardCode" : "";
            string divCond  = divCode  != "0" ? "AND c.DIV_CODE  = :divCode"  : "";

            string sql = $@"
                SELECT ms.MS_DESC, COUNT(*) AS CNT
                  FROM (
                    SELECT WS_CONNNO, MS_CODE,
                           ROW_NUMBER() OVER (PARTITION BY WS_CONNNO ORDER BY WS_FINYR DESC, BC_CODE DESC) AS RN
                      FROM WS.WSMETERREADINGDET
                  ) r
                  JOIN WS.WSMETERSTATUSDET ms ON r.MS_CODE = ms.MS_CODE AND ms.ULB_CODE = 1
                  JOIN WS.WSCONNECTIONDET  c  ON r.WS_CONNNO = c.WS_CONNNO
                 WHERE r.RN = 1
                   {wardCond} {divCond}
                 GROUP BY ms.MS_CODE, ms.MS_DESC
                 ORDER BY ms.MS_CODE";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                AddWardDivParams(cmd, wardCode, divCode);
                var list  = new List<MeterStatusStat>();
                int total = 0;
                using (var dr = await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        int cnt = ToInt(dr["CNT"]);
                        list.Add(new MeterStatusStat { MsDesc = dr["MS_DESC"]?.ToString(), Count = cnt });
                        total += cnt;
                    }
                }
                foreach (var s in list)
                    s.PctShare = total > 0 ? Math.Round((decimal)s.Count / total * 100, 1) : 0;

                result.MeterStatusBreakdown = list;
            }
        }

        public async Task<List<DivisionItem>> GetDivisionsAsync(string wardCode)
        {
            string wardFilter = wardCode != "0" ? "AND d.WARD_CODE = :wardCode" : "";
            string sql = $@"
                SELECT d.DIV_CODE, d.DIV_NAME
                  FROM WS.WSDIVISIONDET d
                 WHERE d.ULB_CODE = 1
                   {wardFilter}
                 ORDER BY d.DIV_CODE";

            using (var conn = _factory.Create())
            {
                await conn.OpenAsync();
                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.BindByName = true;
                    if (wardCode != "0") cmd.Parameters.Add("wardCode", wardCode);
                    var list = new List<DivisionItem>();
                    using (var dr = await cmd.ExecuteReaderAsync())
                    {
                        while (await dr.ReadAsync())
                        {
                            list.Add(new DivisionItem
                            {
                                DivCode = dr["DIV_CODE"]?.ToString(),
                                DivName = dr["DIV_NAME"]?.ToString(),
                            });
                        }
                    }
                    return list;
                }
            }
        }

        // ─── SQL filter helpers ───────────────────────────────────────────────

        private async Task LoadLastYearInsightsAsync(OracleConnection conn, WaterRevenueDashboard result)
        {
            const string prevFinYr = "2025-2026";
            var ins = new LastYearInsights { FinYr = prevFinYr };

            // 1. Summary from wsdemanddet2526 snapshot (pre-closing figures)
            const string sqlSum = @"
                SELECT
                    NVL(SUM(ws_camt), 0)                                          AS WATER_CHARGE,
                    NVL(SUM(NVL(mrent,0)), 0)                                     AS METER_RENT,
                    NVL(SUM(NVL(int_charged,0)), 0)                               AS LATE_FEES,
                    NVL(SUM(ws_chargecolamt), 0)                                  AS WC_COLLECTED,
                    NVL(SUM(NVL(cmrent,0)), 0)                                    AS MR_COLLECTED,
                    NVL(SUM(NVL(int_collected,0)), 0)                             AS LF_COLLECTED,
                    NVL(SUM(ws_cbalamt + NVL(bmrent,0) + NVL(int_balance,0)), 0) AS CARRIED_FWD,
                    COUNT(DISTINCT ws_connno)                                                          AS CONN_BILLED,
                    COUNT(DISTINCT ws_connno) - COUNT(DISTINCT CASE WHEN ws_colflag='0' THEN ws_connno END) AS CONN_CLEARED
                  FROM WS.WSDEMANDDET2526
                 WHERE ws_finyr = :finYr";

            using (var cmd = new OracleCommand(sqlSum, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("finYr", prevFinYr);
                using (var dr = await cmd.ExecuteReaderAsync())
                {
                    if (await dr.ReadAsync())
                    {
                        ins.WaterCharge            = ToDec(dr["WATER_CHARGE"]);
                        ins.MeterRentBilled        = ToDec(dr["METER_RENT"]);
                        ins.LateFeesBilled         = ToDec(dr["LATE_FEES"]);
                        ins.TotalDemand            = ins.WaterCharge + ins.MeterRentBilled + ins.LateFeesBilled;
                        ins.WaterChargeCollected   = ToDec(dr["WC_COLLECTED"]);
                        ins.MeterRentCollected     = ToDec(dr["MR_COLLECTED"]);
                        ins.LateFeesCollected      = ToDec(dr["LF_COLLECTED"]);
                        ins.TotalCollected         = ins.WaterChargeCollected + ins.MeterRentCollected + ins.LateFeesCollected;
                        ins.CarriedForward         = ToDec(dr["CARRIED_FWD"]);
                        ins.ConnectionsBilled      = ToInt(dr["CONN_BILLED"]);
                        ins.ConnectionsCleared     = ToInt(dr["CONN_CLEARED"]);
                        ins.ConnectionsPending     = ins.ConnectionsBilled - ins.ConnectionsCleared;
                        ins.CollectionEfficiencyPct = ins.TotalDemand > 0
                            ? Math.Round(ins.TotalCollected / ins.TotalDemand * 100, 2)
                            : 0;
                    }
                }
            }

            // 2. Billing cycle breakdown from snapshot
            const string sqlBc = @"
                SELECT bc.BC_DESC,
                       COUNT(d.ws_connno)                                                           AS BILLED,
                       NVL(SUM(d.ws_camt + NVL(d.mrent,0) + NVL(d.int_charged,0)), 0)             AS DEMAND,
                       NVL(SUM(d.ws_chargecolamt + NVL(d.cmrent,0) + NVL(d.int_collected,0)), 0)  AS COLLECTED,
                       NVL(SUM(d.ws_cbalamt + NVL(d.bmrent,0) + NVL(d.int_balance,0)), 0)         AS BALANCE
                  FROM WS.WSDEMANDDET2526 d
                  JOIN WS.WSBILLINGCYCLEDET bc ON d.BC_CODE = bc.BC_CODE AND d.ULB_CODE = bc.ULB_CODE
                 WHERE d.ws_finyr = :finYr
                 GROUP BY bc.BC_CODE, bc.BC_DESC
                 ORDER BY bc.BC_CODE DESC";

            using (var cmd = new OracleCommand(sqlBc, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("finYr", prevFinYr);
                var bcList = new List<LastYearBcStat>();
                using (var dr = await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        decimal dmnd = ToDec(dr["DEMAND"]);
                        decimal coll = ToDec(dr["COLLECTED"]);
                        bcList.Add(new LastYearBcStat
                        {
                            BcDesc        = dr["BC_DESC"]?.ToString(),
                            BilledCount   = ToInt(dr["BILLED"]),
                            Demand        = dmnd,
                            Collected     = coll,
                            Balance       = ToDec(dr["BALANCE"]),
                            EfficiencyPct = dmnd > 0 ? Math.Round(coll / dmnd * 100, 1) : 0,
                        });
                    }
                }
                ins.BillingCycles = bcList;
            }

            // 3. Payment methods for FY 2025-26 (from live collection table)
            const string sqlPm = @"
                SELECT ct.COLLTYPE_DESC,
                       COUNT(c.WS_RECEIPTNO)    AS RECEIPTS,
                       NVL(SUM(c.TOTAL_PAID),0) AS AMOUNT
                  FROM WS.WSCOLLECTIONDET_WB c
                  JOIN WS.WSCOLLECTIONTYPEDET ct
                    ON c.COLLTYPE_CODE = ct.COLLTYPE_CODE AND c.ULB_CODE = ct.ULB_CODE
                 WHERE c.CANCELLATIONFLAG = 'n'
                   AND c.STATUS = 'y'
                   AND c.WS_TRANFINYR = :finYr
                 GROUP BY ct.COLLTYPE_CODE, ct.COLLTYPE_DESC
                 ORDER BY AMOUNT DESC";

            using (var cmd = new OracleCommand(sqlPm, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("finYr", prevFinYr);
                var pmList  = new List<PaymentMethodStat>();
                decimal tot = 0;
                using (var dr = await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        var s = new PaymentMethodStat
                        {
                            Method   = dr["COLLTYPE_DESC"]?.ToString(),
                            Receipts = ToInt(dr["RECEIPTS"]),
                            Amount   = ToDec(dr["AMOUNT"]),
                        };
                        pmList.Add(s);
                        tot += s.Amount;
                    }
                }
                foreach (var s in pmList)
                    s.PctShare = tot > 0 ? Math.Round(s.Amount / tot * 100, 1) : 0;
                ins.PaymentMethods = pmList;
            }

            result.LastYearInsights = ins;
        }

        /// <summary>Returns existing helper for connection dashboard — SQL filter helpers start below.</summary>

        /// <summary>
        /// Returns an EXISTS subquery filter rooted on connNoRef to apply ward+div constraints.
        /// e.g. "AND EXISTS (SELECT 1 FROM WS.WSCONNECTIONDET cc WHERE cc.WS_CONNNO=d.WS_CONNNO AND cc.WARD_CODE=:wardCode AND cc.DIV_CODE=:divCode)"
        /// </summary>
        private static string WardDivExistsFilter(string wardCode, string divCode, string connNoRef)
        {
            if (wardCode == "0" && divCode == "0") return "";
            var sb = new System.Text.StringBuilder();
            sb.Append($"AND EXISTS (SELECT 1 FROM WS.WSCONNECTIONDET cc WHERE cc.WS_CONNNO={connNoRef}");
            if (wardCode != "0") sb.Append(" AND cc.WARD_CODE=:wardCode");
            if (divCode  != "0") sb.Append(" AND cc.DIV_CODE=:divCode");
            sb.Append(")");
            return sb.ToString();
        }

        /// <summary>Adds :wardCode and/or :divCode bind parameters if their values are not "0".</summary>
        private static void AddWardDivParams(OracleCommand cmd, string wardCode, string divCode)
        {
            if (wardCode != "0") cmd.Parameters.Add("wardCode", wardCode);
            if (divCode  != "0") cmd.Parameters.Add("divCode",  divCode);
        }

        private static int     ToInt(object v) => v is DBNull ? 0  : Convert.ToInt32(v);
        private static decimal ToDec(object v) => v is DBNull ? 0m : Convert.ToDecimal(v);
    }
}
