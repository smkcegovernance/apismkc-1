using System;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using SmkcApi.Repositories;
using SmkcApi.Models.GAD;

namespace SmkcApi.Repositories.GAD
{
    public class GadBudgetCapRepository : IGadBudgetCapRepository
    {
        private readonly IOracleConnectionFactory _cf;

        public GadBudgetCapRepository(IOracleConnectionFactory cf)
        {
            _cf = cf;
        }

        // ── All budget codes for a fin year (no dept filter) ────────────────────

        public IEnumerable<GadSubheadDto> GetBudgetCodes(string finYear)
        {
            const string sql = @"
                SELECT DISTINCT d.AC_SUBHEAD, d.AC_SUBHEADNAME,
                       d.AC_SUBHEADNAMELL, d.AC_SUBHEADNAMELL_UNICODE
                FROM ABASDEPTBUDJETACTDET a
                JOIN ABASSUBHEADDET d ON a.AC_SUBHEAD = d.AC_SUBHEAD
                WHERE d.AC_SUBHEAD LIKE 'E-%'
                  AND a.STATUS    = 'y'
                  AND d.VALIDFLAG = 'y'
                  AND a.FIN_YEAR  = :fin_year
                ORDER BY d.AC_SUBHEAD";

            var result = new List<GadSubheadDto>();
            using (var conn = _cf.CreateAbas())
            {
                conn.Open();
                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = finYear;
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            result.Add(new GadSubheadDto
                            {
                                AcSubhead              = r["AC_SUBHEAD"].ToString(),
                                AcSubheadName          = r["AC_SUBHEADNAME"] != DBNull.Value ? r["AC_SUBHEADNAME"].ToString() : null,
                                AcSubheadNameLL        = r["AC_SUBHEADNAMELL"] != DBNull.Value ? r["AC_SUBHEADNAMELL"].ToString() : null,
                                AcSubheadNameLLUnicode = r["AC_SUBHEADNAMELL_UNICODE"] != DBNull.Value ? r["AC_SUBHEADNAMELL_UNICODE"].ToString() : null,
                            });
                        }
                    }
                }
            }
            return result;
        }

        // ── List all caps for a fin year ────────────────────────────────────────

        public IEnumerable<BudgetCapDto> ListCaps(int ulbCode, string finYear)
        {
            const string sql = @"
                SELECT c.CAP_ID, c.ULB_CODE, c.AC_SUBHEAD, d.AC_SUBHEADNAME,
                       d.AC_SUBHEADNAMELL, d.AC_SUBHEADNAMELL_UNICODE,
                       c.FIN_YEAR, c.CAP_PERCENTAGE, c.CAP_AMOUNT,
                       c.REMARKS, c.ENT_BY, c.ENT_DT, c.LUP_BY, c.LUP_DATE,
                       NVL(SUM(b.DEBIT_AMOUNT + b.CREDIT_AMOUNT), 0) AS TOTAL_BUDGET
                FROM GAD.GADBUDGETCAPDET c
                LEFT JOIN ABASSUBHEADDET d ON c.AC_SUBHEAD = d.AC_SUBHEAD AND d.VALIDFLAG = 'y'
                LEFT JOIN ABASDEPTBUDJETACTDET b
                       ON c.AC_SUBHEAD = b.AC_SUBHEAD AND c.FIN_YEAR = b.FIN_YEAR
                WHERE c.ULB_CODE  = :ulb_code
                  AND c.FIN_YEAR  = :fin_year
                  AND c.VALIDFLAG = 'y'
                GROUP BY c.CAP_ID, c.ULB_CODE, c.AC_SUBHEAD, d.AC_SUBHEADNAME,
                         d.AC_SUBHEADNAMELL, d.AC_SUBHEADNAMELL_UNICODE,
                         c.FIN_YEAR, c.CAP_PERCENTAGE, c.CAP_AMOUNT,
                         c.REMARKS, c.ENT_BY, c.ENT_DT, c.LUP_BY, c.LUP_DATE
                ORDER BY c.AC_SUBHEAD";

            var result = new List<BudgetCapDto>();
            using (var conn = _cf.CreateAbas())
            {
                conn.Open();
                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                    cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = finYear;
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                            result.Add(MapCap(r));
                    }
                }
            }
            return result;
        }

        // ── Get single cap ─────────────────────────────────────────────────────

        public BudgetCapDto GetCap(int ulbCode, string acSubhead, string finYear)
        {
            const string sql = @"
                SELECT c.CAP_ID, c.ULB_CODE, c.AC_SUBHEAD, d.AC_SUBHEADNAME,
                       d.AC_SUBHEADNAMELL, d.AC_SUBHEADNAMELL_UNICODE,
                       c.FIN_YEAR, c.CAP_PERCENTAGE, c.CAP_AMOUNT,
                       c.REMARKS, c.ENT_BY, c.ENT_DT, c.LUP_BY, c.LUP_DATE,
                       NVL(SUM(b.DEBIT_AMOUNT + b.CREDIT_AMOUNT), 0) AS TOTAL_BUDGET
                FROM GAD.GADBUDGETCAPDET c
                LEFT JOIN ABASSUBHEADDET d ON c.AC_SUBHEAD = d.AC_SUBHEAD AND d.VALIDFLAG = 'y'
                LEFT JOIN ABASDEPTBUDJETACTDET b
                       ON c.AC_SUBHEAD = b.AC_SUBHEAD AND c.FIN_YEAR = b.FIN_YEAR
                WHERE c.ULB_CODE  = :ulb_code
                  AND c.AC_SUBHEAD = :ac_subhead
                  AND c.FIN_YEAR  = :fin_year
                  AND c.VALIDFLAG = 'y'
                GROUP BY c.CAP_ID, c.ULB_CODE, c.AC_SUBHEAD, d.AC_SUBHEADNAME,
                         d.AC_SUBHEADNAMELL, d.AC_SUBHEADNAMELL_UNICODE,
                         c.FIN_YEAR, c.CAP_PERCENTAGE, c.CAP_AMOUNT,
                         c.REMARKS, c.ENT_BY, c.ENT_DT, c.LUP_BY, c.LUP_DATE";

            using (var conn = _cf.CreateAbas())
            {
                conn.Open();
                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                    cmd.Parameters.Add("ac_subhead", OracleDbType.Varchar2).Value = acSubhead;
                    cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = finYear;
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read()) return MapCap(r);
                    }
                }
            }
            return null;
        }

        // ── Upsert ─────────────────────────────────────────────────────────────

        public void UpsertCap(BudgetCapSaveRequest req)
        {
            const string existsSql = @"
                SELECT CAP_ID, CAP_PERCENTAGE, CAP_AMOUNT, REMARKS
                FROM GAD.GADBUDGETCAPDET
                WHERE ULB_CODE = :ulb_code AND AC_SUBHEAD = :ac_subhead AND FIN_YEAR = :fin_year";

            const string insertSql = @"
                INSERT INTO GAD.GADBUDGETCAPDET
                    (CAP_ID, ULB_CODE, AC_SUBHEAD, FIN_YEAR,
                     CAP_PERCENTAGE, CAP_AMOUNT, REMARKS,
                     ENT_BY, ENT_DT, LUP_BY, LUP_DATE, VALIDFLAG)
                VALUES
                    (GAD.SEQGADBUDGETCAP.NEXTVAL, :ulb_code, :ac_subhead, :fin_year,
                     :cap_pct, :cap_amt, :remarks,
                     :action_by, SYSDATE, :action_by, SYSDATE, 'y')
                RETURNING CAP_ID INTO :cap_id_out";

            const string updateSql = @"
                UPDATE GAD.GADBUDGETCAPDET
                   SET CAP_PERCENTAGE = :cap_pct,
                       CAP_AMOUNT     = :cap_amt,
                       REMARKS        = :remarks,
                       LUP_BY         = :action_by,
                       LUP_DATE       = SYSDATE,
                       VALIDFLAG      = 'y'
                 WHERE CAP_ID = :cap_id";

            const string histSql = @"
                INSERT INTO GAD.GADBUDGETCAPHISTORY
                    (HIST_ID, CAP_ID, ULB_CODE, AC_SUBHEAD, FIN_YEAR,
                     OLD_CAP_PCT, OLD_CAP_AMT, NEW_CAP_PCT, NEW_CAP_AMT,
                     OLD_REMARKS, NEW_REMARKS, ACTION, ACTION_BY, ACTION_DT)
                VALUES
                    (GAD.SEQGADCAPHISTORY.NEXTVAL, :cap_id, :ulb_code, :ac_subhead, :fin_year,
                     :old_pct, :old_amt, :new_pct, :new_amt,
                     :old_rem, :new_rem, :action, :action_by, SYSDATE)";

            using (var conn = _cf.CreateAbas())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    long capId = 0;
                    decimal? oldPct = null, oldAmt = null;
                    string oldRem = null;
                    bool isInsert = true;

                    // Check existing
                    using (var cmd = new OracleCommand(existsSql, conn))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = req.UlbCode;
                        cmd.Parameters.Add("ac_subhead", OracleDbType.Varchar2).Value = req.AcSubhead;
                        cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = req.FinYear;
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                isInsert = false;
                                capId = Convert.ToInt64(r["CAP_ID"]);
                                if (r["CAP_PERCENTAGE"] != DBNull.Value) oldPct = Convert.ToDecimal(r["CAP_PERCENTAGE"]);
                                if (r["CAP_AMOUNT"]     != DBNull.Value) oldAmt = Convert.ToDecimal(r["CAP_AMOUNT"]);
                                if (r["REMARKS"]        != DBNull.Value) oldRem = r["REMARKS"].ToString();
                            }
                        }
                    }

                    if (isInsert)
                    {
                        using (var cmd = new OracleCommand(insertSql, conn))
                        {
                            cmd.BindByName = true;
                            cmd.Transaction = tx;
                            AddCapParams(cmd, req);
                            var outParam = cmd.Parameters.Add("cap_id_out", OracleDbType.Decimal);
                            outParam.Direction = System.Data.ParameterDirection.Output;
                            cmd.ExecuteNonQuery();
                            capId = Convert.ToInt64(outParam.Value.ToString());
                        }
                    }
                    else
                    {
                        using (var cmd = new OracleCommand(updateSql, conn))
                        {
                            cmd.Transaction = tx;
                            AddCapNullable(cmd, "cap_pct", req.CapPercentage);
                            AddCapNullable(cmd, "cap_amt", req.CapAmount);
                            cmd.Parameters.Add("remarks",   OracleDbType.NVarchar2).Value  = (object)req.Remarks ?? DBNull.Value;
                            cmd.Parameters.Add("action_by", OracleDbType.Varchar2).Value   = req.ActionBy;
                            cmd.Parameters.Add("cap_id",    OracleDbType.Decimal).Value    = capId;
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // History record
                    using (var cmd = new OracleCommand(histSql, conn))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add("cap_id",    OracleDbType.Decimal).Value   = capId;
                        cmd.Parameters.Add("ulb_code",  OracleDbType.Decimal).Value   = req.UlbCode;
                        cmd.Parameters.Add("ac_subhead",OracleDbType.Varchar2).Value  = req.AcSubhead;
                        cmd.Parameters.Add("fin_year",  OracleDbType.Varchar2).Value  = req.FinYear;
                        AddCapNullable(cmd, "old_pct", oldPct);
                        AddCapNullable(cmd, "old_amt", oldAmt);
                        AddCapNullable(cmd, "new_pct", req.CapPercentage);
                        AddCapNullable(cmd, "new_amt", req.CapAmount);
                        cmd.Parameters.Add("old_rem",   OracleDbType.NVarchar2).Value = (object)oldRem ?? DBNull.Value;
                        cmd.Parameters.Add("new_rem",   OracleDbType.NVarchar2).Value = (object)req.Remarks ?? DBNull.Value;
                        cmd.Parameters.Add("action",    OracleDbType.Varchar2).Value  = isInsert ? "INSERT" : "UPDATE";
                        cmd.Parameters.Add("action_by", OracleDbType.Varchar2).Value  = req.ActionBy;
                        cmd.ExecuteNonQuery();
                    }

                    tx.Commit();
                }
            }
        }

        // ── Soft delete ────────────────────────────────────────────────────────

        public void DeleteCap(int ulbCode, string acSubhead, string finYear, string actionBy)
        {
            const string getSql = @"
                SELECT CAP_ID, CAP_PERCENTAGE, CAP_AMOUNT, REMARKS
                FROM GAD.GADBUDGETCAPDET
                WHERE ULB_CODE = :ulb_code AND AC_SUBHEAD = :ac_subhead AND FIN_YEAR = :fin_year AND VALIDFLAG = 'y'";

            const string delSql = @"
                UPDATE GAD.GADBUDGETCAPDET
                   SET VALIDFLAG = 'n', LUP_BY = :action_by, LUP_DATE = SYSDATE
                 WHERE CAP_ID = :cap_id";

            const string histSql = @"
                INSERT INTO GAD.GADBUDGETCAPHISTORY
                    (HIST_ID, CAP_ID, ULB_CODE, AC_SUBHEAD, FIN_YEAR,
                     OLD_CAP_PCT, OLD_CAP_AMT, NEW_CAP_PCT, NEW_CAP_AMT,
                     OLD_REMARKS, NEW_REMARKS, ACTION, ACTION_BY, ACTION_DT)
                VALUES
                    (GAD.SEQGADCAPHISTORY.NEXTVAL, :cap_id, :ulb_code, :ac_subhead, :fin_year,
                     :old_pct, :old_amt, NULL, NULL,
                     :old_rem, NULL, 'DELETE', :action_by, SYSDATE)";

            using (var conn = _cf.CreateAbas())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    long capId = 0;
                    decimal? oldPct = null, oldAmt = null;
                    string oldRem = null;

                    using (var cmd = new OracleCommand(getSql, conn))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                        cmd.Parameters.Add("ac_subhead", OracleDbType.Varchar2).Value = acSubhead;
                        cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = finYear;
                        using (var r = cmd.ExecuteReader())
                        {
                            if (!r.Read()) return; // nothing to delete
                            capId = Convert.ToInt64(r["CAP_ID"]);
                            if (r["CAP_PERCENTAGE"] != DBNull.Value) oldPct = Convert.ToDecimal(r["CAP_PERCENTAGE"]);
                            if (r["CAP_AMOUNT"]     != DBNull.Value) oldAmt = Convert.ToDecimal(r["CAP_AMOUNT"]);
                            if (r["REMARKS"]        != DBNull.Value) oldRem = r["REMARKS"].ToString();
                        }
                    }

                    using (var cmd = new OracleCommand(delSql, conn))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add("action_by", OracleDbType.Varchar2).Value = actionBy;
                        cmd.Parameters.Add("cap_id",    OracleDbType.Decimal).Value  = capId;
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = new OracleCommand(histSql, conn))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add("cap_id",    OracleDbType.Decimal).Value   = capId;
                        cmd.Parameters.Add("ulb_code",  OracleDbType.Decimal).Value   = ulbCode;
                        cmd.Parameters.Add("ac_subhead",OracleDbType.Varchar2).Value  = acSubhead;
                        cmd.Parameters.Add("fin_year",  OracleDbType.Varchar2).Value  = finYear;
                        AddCapNullable(cmd, "old_pct", oldPct);
                        AddCapNullable(cmd, "old_amt", oldAmt);
                        cmd.Parameters.Add("old_rem",   OracleDbType.NVarchar2).Value = (object)oldRem ?? DBNull.Value;
                        cmd.Parameters.Add("action_by", OracleDbType.Varchar2).Value  = actionBy;
                        cmd.ExecuteNonQuery();
                    }

                    tx.Commit();
                }
            }
        }

        // ── History ────────────────────────────────────────────────────────────

        public IEnumerable<BudgetCapHistoryDto> GetHistory(string acSubhead, string finYear)
        {
            const string sql = @"
                SELECT HIST_ID, CAP_ID, ULB_CODE, AC_SUBHEAD, FIN_YEAR,
                       OLD_CAP_PCT, OLD_CAP_AMT, NEW_CAP_PCT, NEW_CAP_AMT,
                       OLD_REMARKS, NEW_REMARKS, ACTION, ACTION_BY, ACTION_DT
                FROM GAD.GADBUDGETCAPHISTORY
                WHERE AC_SUBHEAD = :ac_subhead AND FIN_YEAR = :fin_year
                ORDER BY ACTION_DT DESC";

            var result = new List<BudgetCapHistoryDto>();
            using (var conn = _cf.CreateAbas())
            {
                conn.Open();
                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add("ac_subhead", OracleDbType.Varchar2).Value = acSubhead;
                    cmd.Parameters.Add("fin_year",   OracleDbType.Varchar2).Value = finYear;
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            result.Add(new BudgetCapHistoryDto
                            {
                                HistId    = Convert.ToInt64(r["HIST_ID"]),
                                CapId     = Convert.ToInt64(r["CAP_ID"]),
                                AcSubhead = r["AC_SUBHEAD"].ToString(),
                                FinYear   = r["FIN_YEAR"].ToString(),
                                OldCapPct = r["OLD_CAP_PCT"] != DBNull.Value ? (decimal?)Convert.ToDecimal(r["OLD_CAP_PCT"]) : null,
                                OldCapAmt = r["OLD_CAP_AMT"] != DBNull.Value ? (decimal?)Convert.ToDecimal(r["OLD_CAP_AMT"]) : null,
                                NewCapPct = r["NEW_CAP_PCT"] != DBNull.Value ? (decimal?)Convert.ToDecimal(r["NEW_CAP_PCT"]) : null,
                                NewCapAmt = r["NEW_CAP_AMT"] != DBNull.Value ? (decimal?)Convert.ToDecimal(r["NEW_CAP_AMT"]) : null,
                                OldRemarks = r["OLD_REMARKS"] != DBNull.Value ? r["OLD_REMARKS"].ToString() : null,
                                NewRemarks = r["NEW_REMARKS"] != DBNull.Value ? r["NEW_REMARKS"].ToString() : null,
                                Action    = r["ACTION"].ToString(),
                                ActionBy  = r["ACTION_BY"].ToString(),
                                ActionDt  = Convert.ToDateTime(r["ACTION_DT"]),
                            });
                        }
                    }
                }
            }
            return result;
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static BudgetCapDto MapCap(OracleDataReader r)
        {
            decimal totalBudget = r["TOTAL_BUDGET"] != DBNull.Value ? Convert.ToDecimal(r["TOTAL_BUDGET"]) : 0m;
            decimal? capPct = r["CAP_PERCENTAGE"] != DBNull.Value ? (decimal?)Convert.ToDecimal(r["CAP_PERCENTAGE"]) : null;
            decimal? capAmt = r["CAP_AMOUNT"]     != DBNull.Value ? (decimal?)Convert.ToDecimal(r["CAP_AMOUNT"])     : null;

            decimal effective = totalBudget;
            if (capAmt.HasValue && capAmt.Value > 0)
                effective = capAmt.Value;
            else if (capPct.HasValue && capPct.Value > 0)
                effective = Math.Round(totalBudget * capPct.Value / 100m, 0);

            string name = null;
            if (r["AC_SUBHEADNAMELL_UNICODE"] != DBNull.Value) name = r["AC_SUBHEADNAMELL_UNICODE"].ToString();
            if (string.IsNullOrEmpty(name) && r["AC_SUBHEADNAMELL"] != DBNull.Value) name = r["AC_SUBHEADNAMELL"].ToString();
            if (string.IsNullOrEmpty(name) && r["AC_SUBHEADNAME"]   != DBNull.Value) name = r["AC_SUBHEADNAME"].ToString();

            return new BudgetCapDto
            {
                CapId          = Convert.ToInt64(r["CAP_ID"]),
                UlbCode        = Convert.ToInt32(r["ULB_CODE"]),
                AcSubhead      = r["AC_SUBHEAD"].ToString(),
                AcSubheadName  = name,
                FinYear        = r["FIN_YEAR"].ToString(),
                CapPercentage  = capPct,
                CapAmount      = capAmt,
                TotalBudget    = totalBudget,
                EffectiveBudget= effective,
                Remarks        = r["REMARKS"] != DBNull.Value ? r["REMARKS"].ToString() : null,
                EntBy          = r["ENT_BY"]  != DBNull.Value ? r["ENT_BY"].ToString()  : null,
                EntDt          = r["ENT_DT"]  != DBNull.Value ? (DateTime?)Convert.ToDateTime(r["ENT_DT"]) : null,
                LupBy          = r["LUP_BY"]  != DBNull.Value ? r["LUP_BY"].ToString()  : null,
                LupDate        = r["LUP_DATE"]!= DBNull.Value ? (DateTime?)Convert.ToDateTime(r["LUP_DATE"]) : null,
            };
        }

        private static void AddCapParams(OracleCommand cmd, BudgetCapSaveRequest req)
        {
            cmd.Parameters.Add("ulb_code",  OracleDbType.Decimal).Value  = req.UlbCode;
            cmd.Parameters.Add("ac_subhead",OracleDbType.Varchar2).Value = req.AcSubhead;
            cmd.Parameters.Add("fin_year",  OracleDbType.Varchar2).Value = req.FinYear;
            AddCapNullable(cmd, "cap_pct", req.CapPercentage);
            AddCapNullable(cmd, "cap_amt", req.CapAmount);
            cmd.Parameters.Add("remarks",   OracleDbType.NVarchar2).Value = (object)req.Remarks ?? DBNull.Value;
            cmd.Parameters.Add("action_by", OracleDbType.Varchar2).Value  = req.ActionBy;
        }

        private static void AddCapNullable(OracleCommand cmd, string name, decimal? value)
        {
            var p = cmd.Parameters.Add(name, OracleDbType.Decimal);
            p.Value = value.HasValue ? (object)value.Value : DBNull.Value;
        }
    }
}
