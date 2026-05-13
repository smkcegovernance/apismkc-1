using System;
using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace SmkcApi.Repositories.GAD
{
    // =========================================================================
    // BudgetBalanceDataAccess
    //
    // Provides two ways to query available budget balance with cap applied:
    //
    //   1. GetBalanceAmt()  — matches the Oracle function signature exactly.
    //                         Returns a DataSet with a single row containing
    //                         TotalBudget, CapPercentage, CapAmount,
    //                         EffectiveBudget, CommittedAmt, AvailableBalance.
    //
    //   2. GetBalanceAmtByView() — single-query approach using the
    //                         VW_BUDGET_CAP_BALANCE view (faster, same result).
    //
    // Usage (legacy style):
    //   var dal  = new BudgetBalanceDataAccess("User Id=abas;...");
    //   DataSet ds = dal.GetBalanceAmt("E-4201", 100000, "2026-2027", "01");
    //   decimal available = Convert.ToDecimal(ds.Tables[0].Rows[0]["AVAILABLE_BALANCE"]);
    // =========================================================================

    public class BudgetBalanceDataAccess
    {
        // ── Instance fields (matches LoadUserFund style) ──────────────────────
        private DataSet            m_ds;
        private OracleConnection   ocon;
        private OracleDataAdapter  oda;

        public BudgetBalanceDataAccess(string connectionString)
        {
            ocon = new OracleConnection(connectionString);
        }

        // ── Method 1: multi-query, mirrors Oracle GetBalanceAmt function ──────
        //
        // Parameters match the Oracle function signature:
        //   AcHead      — budget account subhead code  e.g. "E-4201"
        //   DrAmt       — total sanctioned debit amount (pass 0 to skip committed calc)
        //   strFinyr    — financial year string         e.g. "2026-2027"
        //   strFundcode — fund code                     e.g. "01"
        //
        // Returns DataSet with one DataTable, one row, columns:
        //   AC_SUBHEAD | FIN_YEAR | TOTAL_BUDGET | CAP_PERCENTAGE | CAP_AMOUNT
        //   EFFECTIVE_BUDGET | COMMITTED_AMT | AVAILABLE_BALANCE
        // ─────────────────────────────────────────────────────────────────────

        public DataSet GetBalanceAmt(string AcHead, decimal DrAmt,
                                     string strFinyr, string strFundcode)
        {
            m_ds = new DataSet();

            decimal totalBudget     = 0;
            decimal committedAmt    = 0;
            decimal capPercentage   = 0;
            decimal capAmount       = 0;
            bool    hasCapPct       = false;
            bool    hasCapAmt       = false;

            ocon.Open();
            try
            {
                // ── Step 1: Get total sanctioned budget ───────────────────────
                if (DrAmt != 0)
                {
                    string sqlBudget = @"SELECT NVL(DEBIT_AMOUNT, 0) AS DEBIT_AMOUNT
                                           FROM ABASDEPTBUDJETACTDET
                                          WHERE ULB_CODE   = '1'
                                            AND FIN_YEAR   = :fin_year
                                            AND AC_SUBHEAD = :ac_subhead";

                    using (var cmd = new OracleCommand(sqlBudget, ocon))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("fin_year",   OracleDbType.Varchar2).Value = strFinyr;
                        cmd.Parameters.Add("ac_subhead", OracleDbType.Varchar2).Value = AcHead;

                        object val = cmd.ExecuteScalar();
                        if (val != null && val != DBNull.Value)
                            totalBudget = Convert.ToDecimal(val);
                    }

                    // ── Step 2: Get committed amount from bills ───────────────
                    string sqlCommitted = @"
                        SELECT NVL(SUM(final_amt), 0) AS COMMITTED_AMT
                          FROM (
                            SELECT CASE
                                     WHEN bd.ABOVEBELOWTYPE = 1
                                       THEN base_amt + (base_amt * bd.CONTRACTORPER / 100)
                                     WHEN bd.ABOVEBELOWTYPE = 2
                                       THEN base_amt - (base_amt * bd.CONTRACTORPER / 100)
                                     ELSE base_amt
                                   END + item2_amt AS final_amt
                              FROM (
                                SELECT bd.ABOVEBELOWTYPE,
                                       bd.CONTRACTORPER,
                                       SUM(CASE WHEN rt.ITEMTYPE != 2
                                                THEN rt.RATE * rt.QUANTITY ELSE 0 END) AS base_amt,
                                       SUM(CASE WHEN rt.ITEMTYPE  = 2
                                                THEN rt.RATE * rt.QUANTITY ELSE 0 END) AS item2_amt
                                  FROM OBREGULARBILLTRANDET rt
                                  JOIN OBREGULARBILLDET bd ON rt.REGBILLNO = bd.REGBILLNO
                                 WHERE bd.BUDGETCODE           = :ac_subhead
                                   AND GETFINYEAR(bd.BILLDATE) = :fin_year
                                 GROUP BY bd.ABOVEBELOWTYPE, bd.CONTRACTORPER
                              ) bd
                          )";

                    using (var cmd = new OracleCommand(sqlCommitted, ocon))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("ac_subhead", OracleDbType.Varchar2).Value = AcHead;
                        cmd.Parameters.Add("fin_year",   OracleDbType.Varchar2).Value = strFinyr;

                        object val = cmd.ExecuteScalar();
                        if (val != null && val != DBNull.Value)
                            committedAmt = Convert.ToDecimal(val);
                    }
                }

                // ── Step 3: Get budget cap settings ──────────────────────────
                string sqlCap = @"SELECT NVL(CAP_PERCENTAGE, -1) AS CAP_PERCENTAGE,
                                         NVL(CAP_AMOUNT,     -1) AS CAP_AMOUNT
                                    FROM GAD.GADBUDGETCAPDET
                                   WHERE ULB_CODE   = 1
                                     AND AC_SUBHEAD = :ac_subhead
                                     AND FIN_YEAR   = :fin_year
                                     AND VALIDFLAG  = 'y'";

                using (var cmd = new OracleCommand(sqlCap, ocon))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("ac_subhead", OracleDbType.Varchar2).Value = AcHead;
                    cmd.Parameters.Add("fin_year",   OracleDbType.Varchar2).Value = strFinyr;

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            decimal pct = reader.IsDBNull(0) ? -1 : reader.GetDecimal(0);
                            decimal amt = reader.IsDBNull(1) ? -1 : reader.GetDecimal(1);

                            if (pct >= 0) { capPercentage = pct; hasCapPct = true; }
                            if (amt >= 0) { capAmount     = amt; hasCapAmt = true; }
                        }
                    }
                }
            }
            finally
            {
                ocon.Close();
            }

            // ── Step 4: Compute effective budget and available balance ────────
            decimal effectiveBudget;
            if (hasCapAmt)
                effectiveBudget = Math.Min(totalBudget, capAmount);          // explicit amount cap
            else if (hasCapPct)
                effectiveBudget = totalBudget * capPercentage / 100m;        // percentage cap
            else
                effectiveBudget = totalBudget;                               // no cap

            decimal availableBalance = effectiveBudget - committedAmt;

            // ── Step 5: Build result DataTable ───────────────────────────────
            DataTable dt = new DataTable("BudgetBalance");
            dt.Columns.Add("AC_SUBHEAD",        typeof(string));
            dt.Columns.Add("FIN_YEAR",          typeof(string));
            dt.Columns.Add("TOTAL_BUDGET",      typeof(decimal));
            dt.Columns.Add("CAP_PERCENTAGE",    typeof(decimal));
            dt.Columns.Add("CAP_AMOUNT",        typeof(decimal));
            dt.Columns.Add("EFFECTIVE_BUDGET",  typeof(decimal));
            dt.Columns.Add("COMMITTED_AMT",     typeof(decimal));
            dt.Columns.Add("AVAILABLE_BALANCE", typeof(decimal));

            DataRow row = dt.NewRow();
            row["AC_SUBHEAD"]        = AcHead;
            row["FIN_YEAR"]          = strFinyr;
            row["TOTAL_BUDGET"]      = totalBudget;
            row["CAP_PERCENTAGE"]    = hasCapPct ? (object)capPercentage : DBNull.Value;
            row["CAP_AMOUNT"]        = hasCapAmt ? (object)capAmount     : DBNull.Value;
            row["EFFECTIVE_BUDGET"]  = effectiveBudget;
            row["COMMITTED_AMT"]     = committedAmt;
            row["AVAILABLE_BALANCE"] = availableBalance;
            dt.Rows.Add(row);

            m_ds.Tables.Add(dt);
            return m_ds;
        }

        // ── Method 2: single-query via VW_BUDGET_CAP_BALANCE view ─────────────
        //
        // Simpler and faster — one query against the pre-built view.
        // Use this when you only need the balance for display (not bill validation).
        //
        // Overload A: single account head
        //   DataSet ds = dal.GetBalanceAmtByView("E-4201", "2026-2027");
        //
        // Overload B: all heads for a fin year
        //   DataSet ds = dal.GetBalanceAmtByView(null, "2026-2027");
        // ─────────────────────────────────────────────────────────────────────

        public DataSet GetBalanceAmtByView(string AcHead, string strFinyr)
        {
            m_ds = new DataSet();

            string whereAcHead = string.IsNullOrEmpty(AcHead)
                ? ""
                : "AND AC_SUBHEAD = :ac_subhead";

            string sql = @"SELECT ULB_CODE,
                                  AC_SUBHEAD,
                                  AC_SUBHEADNAME,
                                  FIN_YEAR,
                                  TOTAL_BUDGET,
                                  CAP_PERCENTAGE,
                                  CAP_AMOUNT,
                                  EFFECTIVE_BUDGET,
                                  COMMITTED_AMT,
                                  AVAILABLE_BALANCE
                             FROM VW_BUDGET_CAP_BALANCE
                            WHERE ULB_CODE = '1'
                              AND FIN_YEAR = :fin_year
                              " + whereAcHead + @"
                            ORDER BY AC_SUBHEAD";

            ocon.Open();
            oda = new OracleDataAdapter(sql, ocon);
            oda.SelectCommand.BindByName = true;
            oda.SelectCommand.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = strFinyr;

            if (!string.IsNullOrEmpty(AcHead))
                oda.SelectCommand.Parameters.Add("ac_subhead", OracleDbType.Varchar2).Value = AcHead;

            oda.Fill(m_ds, "BudgetBalance");
            ocon.Close();
            return m_ds;
        }

        // ── Convenience: returns just the available balance as a scalar ────────
        //
        //   decimal balance = dal.GetAvailableBalance("E-4201", "2026-2027");
        // ─────────────────────────────────────────────────────────────────────

        public decimal GetAvailableBalance(string AcHead, string strFinyr)
        {
            DataSet ds = GetBalanceAmtByView(AcHead, strFinyr);
            if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                return 0m;

            object val = ds.Tables[0].Rows[0]["AVAILABLE_BALANCE"];
            return (val == null || val == DBNull.Value) ? 0m : Convert.ToDecimal(val);
        }

        // ── Dispose ───────────────────────────────────────────────────────────

        public void Dispose()
        {
            if (ocon != null && ocon.State == ConnectionState.Open)
                ocon.Close();
            ocon?.Dispose();
            oda?.Dispose();
        }
    }
}
