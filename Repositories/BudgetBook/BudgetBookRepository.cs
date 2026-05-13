using System;
using System.Collections.Generic;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using SmkcApi.Models.BudgetBook;

namespace SmkcApi.Repositories.BudgetBook
{
    public class BudgetBookRepository : IBudgetBookRepository
    {
        private readonly IOracleConnectionFactory _connectionFactory;

        public BudgetBookRepository(IOracleConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        // ── Departments ──────────────────────────────────────────────────────────

        public IEnumerable<DepartmentDto> GetDepartments(int ulbCode)
        {
            const string sql = @"
                SELECT DEPT_CODE, DEPT_NAME, DEPT_NAMELL, DEPT_NAMELL_UNICODE
                FROM ULBERP.DEPARTMENTDET
                WHERE ULB_CODE = :ulb_code AND VALIDFLAG = 'y' AND STATUS = 'y'
                ORDER BY DEPT_NAME";

            var result = new List<DepartmentDto>();
            using (var conn = _connectionFactory.CreateAbas())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new DepartmentDto
                        {
                            DeptCode = Convert.ToInt32(reader["DEPT_CODE"]),
                            DeptName = reader["DEPT_NAME"] as string ?? string.Empty,
                            DeptNameLL = reader["DEPT_NAMELL"] as string ?? string.Empty,
                            DeptNameLLUnicode = reader["DEPT_NAMELL_UNICODE"] as string ?? string.Empty,
                        });
                    }
                }
            }
            return result;
        }

        // ── Subheads with budget ─────────────────────────────────────────────────

        public IEnumerable<SubheadDto> GetSubheads(int ulbCode, string finYear)
        {
            const string sql = @"
                SELECT s.AC_SUBHEAD,
                       s.AC_SUBHEADNAME,
                       s.AC_SUBHEADNAMELL,
                       s.AC_SUBHEADNAMELL_UNICODE,
                       NVL(SUM(d.DEBIT_AMOUNT + d.CREDIT_AMOUNT), 0) AS TOTAL_BUDGET
                FROM ABASSUBHEADDET s
                LEFT JOIN ABASDEPTBUDJETACTDET d
                       ON s.AC_SUBHEAD  = d.AC_SUBHEAD
                      AND d.ULB_CODE    = s.ULB_CODE
                      AND d.FIN_YEAR    = :fin_year
                WHERE s.ULB_CODE = :ulb_code
                  AND s.VALIDFLAG = 'y'
                GROUP BY s.AC_SUBHEAD, s.AC_SUBHEADNAME, s.AC_SUBHEADNAMELL, s.AC_SUBHEADNAMELL_UNICODE
                HAVING NVL(SUM(d.DEBIT_AMOUNT + d.CREDIT_AMOUNT), 0) > 0
                ORDER BY s.AC_SUBHEAD";

            var result = new List<SubheadDto>();
            using (var conn = _connectionFactory.CreateAbas())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = finYear;
                cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new SubheadDto
                        {
                            AcSubhead = reader["AC_SUBHEAD"] as string ?? string.Empty,
                            AcSubheadName = reader["AC_SUBHEADNAME"] as string ?? string.Empty,
                            AcSubheadNameLL = reader["AC_SUBHEADNAMELL"] as string ?? string.Empty,
                            AcSubheadNameLLUnicode = reader["AC_SUBHEADNAMELL_UNICODE"] as string ?? string.Empty,
                            TotalBudget = reader["TOTAL_BUDGET"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["TOTAL_BUDGET"]),
                        });
                    }
                }
            }
            return result;
        }

        // ── Remaining budget ─────────────────────────────────────────────────────

        public BudgetRemainingDto GetRemainingBudget(int ulbCode, string acSubhead, string finYear)
        {
            const string budgetSql = @"
                SELECT NVL(SUM(DEBIT_AMOUNT + CREDIT_AMOUNT), 0) AS TOTAL_BUDGET
                FROM ABASDEPTBUDJETACTDET
                WHERE ULB_CODE = :ulb_code AND AC_SUBHEAD = :ac_subhead AND FIN_YEAR = :fin_year";

            // Committed = GAD quotations + GAD tenders + existing budget book entries (all non-cancelled)
            const string committedSql = @"
                SELECT
                    NVL((SELECT SUM(PROPOSEDCOST) FROM GAD.GADQUOTATIONORDERSDET
                         WHERE ULB_CODE = :ulb_code AND ACHEAD_CODE = :ac_subhead
                           AND FINYEAR = :fin_year AND VALIDFLAG = 'y'), 0)
                  + NVL((SELECT SUM(PROPOSEDCOST) FROM GAD.GADTENDERORDERSDET
                         WHERE ULB_CODE = :ulb_code AND ACHEAD_CODE = :ac_subhead
                           AND FINYEAR = :fin_year AND VALIDFLAG = 'y'), 0)
                  + NVL((SELECT SUM(PROPOSEDWORK_AMOUNT) FROM ABASBUDGETBOOKENTRY
                         WHERE ULB_CODE = :ulb_code AND AC_SUBHEAD = :ac_subhead
                           AND FIN_YEAR = :fin_year
                           AND (CANCELLED IS NULL OR UPPER(CANCELLED) <> 'Y')), 0)
                AS COMMITTED FROM DUAL";

            const string capSql = @"
                SELECT CAP_PERCENTAGE, CAP_AMOUNT
                FROM GAD.GADBUDGETCAPDET
                WHERE AC_SUBHEAD = :ac_subhead AND FIN_YEAR = :fin_year
                  AND ULB_CODE = :ulb_code AND VALIDFLAG = 'y'";

            decimal totalBudget = 0m;
            decimal committed = 0m;
            decimal? capPct = null;
            decimal? capAmt = null;

            using (var conn = _connectionFactory.CreateAbas())
            {
                conn.Open();
                using (var cmd = new OracleCommand(budgetSql, conn))
                {
                    cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                    cmd.Parameters.Add("ac_subhead", OracleDbType.Varchar2).Value = acSubhead;
                    cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = finYear;
                    var val = cmd.ExecuteScalar();
                    if (val != null && val != DBNull.Value)
                        totalBudget = Convert.ToDecimal(val);
                }

                using (var cmd = new OracleCommand(committedSql, conn))
                {
                    cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                    cmd.Parameters.Add("ac_subhead", OracleDbType.Varchar2).Value = acSubhead;
                    cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = finYear;
                    var val = cmd.ExecuteScalar();
                    if (val != null && val != DBNull.Value)
                        committed = Convert.ToDecimal(val);
                }

                try
                {
                    using (var cmd = new OracleCommand(capSql, conn))
                    {
                        cmd.Parameters.Add("ac_subhead", OracleDbType.Varchar2).Value = acSubhead;
                        cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = finYear;
                        cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                if (r["CAP_PERCENTAGE"] != DBNull.Value) capPct = Convert.ToDecimal(r["CAP_PERCENTAGE"]);
                                if (r["CAP_AMOUNT"]     != DBNull.Value) capAmt = Convert.ToDecimal(r["CAP_AMOUNT"]);
                            }
                        }
                    }
                }
                catch { /* cap table may not exist */ }
            }

            decimal effectiveBudget = totalBudget;
            if (capAmt.HasValue && capAmt.Value > 0)
                effectiveBudget = capAmt.Value;
            else if (capPct.HasValue && capPct.Value > 0)
                effectiveBudget = Math.Round(totalBudget * capPct.Value / 100m, 0);

            return new BudgetRemainingDto
            {
                AcSubhead = acSubhead,
                FinYear = finYear,
                TotalBudget = totalBudget,
                EffectiveBudget = effectiveBudget,
                CapPercentage = capPct,
                CapAmount = capAmt,
                CommittedAmount = committed,
                RemainingBudget = effectiveBudget - committed,
            };
        }

        // ── Save primary entry ───────────────────────────────────────────────────

        public BudgetBookSaveResult SavePrimaryEntry(int ulbCode, PrimaryBudgetEntryRequest request)
        {
            // Fetch current remaining inside a transaction to avoid race conditions
            const string budgetSql = @"
                SELECT NVL(SUM(DEBIT_AMOUNT + CREDIT_AMOUNT), 0)
                FROM ABASDEPTBUDJETACTDET
                WHERE ULB_CODE = :ulb_code AND AC_SUBHEAD = :ac_subhead AND FIN_YEAR = :fin_year";

            // Lock the subhead row to serialise concurrent entries for the same subhead
            const string lockSubheadSql = @"
                SELECT AC_SUBHEAD FROM ABASSUBHEADDET
                WHERE AC_SUBHEAD = :ac_subhead AND ULB_CODE = :ulb_code
                FOR UPDATE";

            // Committed = GAD quotations + GAD tenders + existing budget book entries (all non-cancelled)
            const string committedSql = @"
                SELECT
                    NVL((SELECT SUM(PROPOSEDCOST) FROM GAD.GADQUOTATIONORDERSDET
                         WHERE ULB_CODE = :ulb_code AND ACHEAD_CODE = :ac_subhead
                           AND FINYEAR = :fin_year AND VALIDFLAG = 'y'), 0)
                  + NVL((SELECT SUM(PROPOSEDCOST) FROM GAD.GADTENDERORDERSDET
                         WHERE ULB_CODE = :ulb_code AND ACHEAD_CODE = :ac_subhead
                           AND FINYEAR = :fin_year AND VALIDFLAG = 'y'), 0)
                  + NVL((SELECT SUM(PROPOSEDWORK_AMOUNT) FROM ABASBUDGETBOOKENTRY
                         WHERE ULB_CODE = :ulb_code AND AC_SUBHEAD = :ac_subhead
                           AND FIN_YEAR = :fin_year
                           AND (CANCELLED IS NULL OR UPPER(CANCELLED) <> 'Y')), 0)
                AS COMMITTED FROM DUAL";

            const string insertSql = @"
                INSERT INTO ABASBUDGETBOOKENTRY
                    (BOOKENTRYNO, ULB_CODE, FIN_YEAR, AC_HEAD, AC_SUBHEAD,
                     PROPOSEDWORK_AMOUNT, FINAL_PROPOSEDWORK_AMOUNT,
                     BUDGET_AMOUNT, REMAINING_BUDGET_AMOUNT,
                     BUDGETENTBY, BUDGETENTRYDATE,
                     STATUS, ENTRYTYPE, CANCELLED, FINALBOOKENTRYNO,
                     DEPT_CODE, WORK_NAME, NASTINO, FILETYPE)
                VALUES
                    (SEQBOOKENTRY_NO.NEXTVAL, :ulb_code, :fin_year, '0', :ac_subhead,
                     :proposed_amount, 0,
                     :budget_amount, :remaining_budget,
                     :entered_by, SYSDATE,
                     'n', 'n', 'n', 0,
                     :dept_code, :work_name, :nastino, :filetype)
                RETURNING BOOKENTRYNO INTO :book_entry_no";

            using (var conn = _connectionFactory.CreateAbas())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        decimal totalBudget = 0m;
                        decimal committed = 0m;

                        // Acquire row-level lock on the subhead to serialise concurrent entries
                        using (var cmd = new OracleCommand(lockSubheadSql, conn))
                        {
                            cmd.Transaction = tx;
                            cmd.Parameters.Add("ac_subhead", OracleDbType.Varchar2).Value = request.AcSubhead;
                            cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                            cmd.ExecuteScalar();
                        }

                        using (var cmd = new OracleCommand(budgetSql, conn))
                        {
                            cmd.Transaction = tx;
                            cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                            cmd.Parameters.Add("ac_subhead", OracleDbType.Varchar2).Value = request.AcSubhead;
                            cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = request.FinYear;
                            var val = cmd.ExecuteScalar();
                            if (val != null && val != DBNull.Value)
                                totalBudget = Convert.ToDecimal(val);
                        }

                        using (var cmd = new OracleCommand(committedSql, conn))
                        {
                            cmd.Transaction = tx;
                            cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                            cmd.Parameters.Add("ac_subhead", OracleDbType.Varchar2).Value = request.AcSubhead;
                            cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = request.FinYear;
                            var val = cmd.ExecuteScalar();
                            if (val != null && val != DBNull.Value)
                                committed = Convert.ToDecimal(val);
                        }

                        // Apply cap (if any) to compute effective budget
                        decimal effectiveBudget = totalBudget;
                        const string capSqlInner = @"
                            SELECT CAP_PERCENTAGE, CAP_AMOUNT
                            FROM GAD.GADBUDGETCAPDET
                            WHERE AC_SUBHEAD = :ac_subhead AND FIN_YEAR = :fin_year
                              AND ULB_CODE = :ulb_code AND VALIDFLAG = 'y'";
                        try
                        {
                            using (var cmd = new OracleCommand(capSqlInner, conn))
                            {
                                cmd.Transaction = tx;
                                cmd.Parameters.Add("ac_subhead", OracleDbType.Varchar2).Value = request.AcSubhead;
                                cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = request.FinYear;
                                cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                                using (var r = cmd.ExecuteReader())
                                {
                                    if (r.Read())
                                    {
                                        decimal? capPct = r["CAP_PERCENTAGE"] != DBNull.Value ? (decimal?)Convert.ToDecimal(r["CAP_PERCENTAGE"]) : null;
                                        decimal? capAmt = r["CAP_AMOUNT"]     != DBNull.Value ? (decimal?)Convert.ToDecimal(r["CAP_AMOUNT"])     : null;
                                        if (capAmt.HasValue && capAmt.Value > 0)
                                            effectiveBudget = capAmt.Value;
                                        else if (capPct.HasValue && capPct.Value > 0)
                                            effectiveBudget = Math.Round(totalBudget * capPct.Value / 100m, 0);
                                    }
                                }
                            }
                        }
                        catch { /* cap table may not exist */ }

                        decimal remaining = effectiveBudget - committed;

                        if (request.ProposedAmount > remaining)
                        {
                            tx.Rollback();
                            return new BudgetBookSaveResult
                            {
                                Success = false,
                                Message = string.Format(
                                    "अपुरा तरतूद: लेखाशीर्षात शिल्लक रक्कम ₹{0:N0} असून प्रस्तावित रक्कम ₹{1:N0} जास्त आहे.",
                                    remaining, request.ProposedAmount),
                                RemainingBudget = remaining,
                            };
                        }

                        long newBookEntryNo = 0;
                        using (var cmd = new OracleCommand(insertSql, conn))
                        {
                            cmd.Transaction = tx;
                            cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                            cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = request.FinYear;
                            cmd.Parameters.Add("ac_subhead", OracleDbType.Varchar2).Value = request.AcSubhead;
                            cmd.Parameters.Add("proposed_amount", OracleDbType.Decimal).Value = request.ProposedAmount;
                            cmd.Parameters.Add("budget_amount", OracleDbType.Decimal).Value = totalBudget;
                            cmd.Parameters.Add("remaining_budget", OracleDbType.Decimal).Value = remaining - request.ProposedAmount;
                            cmd.Parameters.Add("entered_by", OracleDbType.Varchar2).Value = TruncateString(request.EnteredBy, 8);
                            cmd.Parameters.Add("dept_code", OracleDbType.Decimal).Value = request.DeptCode;
                            cmd.Parameters.Add("work_name", OracleDbType.Varchar2).Value = request.WorkName ?? string.Empty;
                            cmd.Parameters.Add("nastino", OracleDbType.Decimal).Value = request.NastiNo;
                            cmd.Parameters.Add("filetype", OracleDbType.Varchar2).Value = request.FileType ?? string.Empty;

                            var retParam = cmd.Parameters.Add("book_entry_no", OracleDbType.Decimal);
                            retParam.Direction = ParameterDirection.Output;

                            cmd.ExecuteNonQuery();
                            newBookEntryNo = Convert.ToInt64(retParam.Value.ToString());
                        }

                        tx.Commit();
                        return new BudgetBookSaveResult
                        {
                            Success = true,
                            Message = "प्राथमिक तरतूद नोंद यशस्वीरित्या जतन झाली.",
                            BookEntryNo = newBookEntryNo,
                            RemainingBudget = remaining - request.ProposedAmount,
                        };
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        return new BudgetBookSaveResult
                        {
                            Success = false,
                            Message = "त्रुटी: " + ex.Message,
                        };
                    }
                }
            }
        }

        // ── Get single primary entry ─────────────────────────────────────────────

        public BudgetBookEntryDto GetPrimaryEntry(int ulbCode, long bookEntryNo)
        {
            const string sql = @"
                SELECT b.BOOKENTRYNO, b.FINALBOOKENTRYNO, b.FIN_YEAR, b.AC_HEAD, b.AC_SUBHEAD,
                       b.PROPOSEDWORK_AMOUNT, b.FINAL_PROPOSEDWORK_AMOUNT,
                       b.BUDGET_AMOUNT, b.REMAINING_BUDGET_AMOUNT,
                       b.BUDGETENTBY, b.BUDGETENTRYDATE,
                       b.FINALBUDGETENTBY, b.FINALBUDGETENTRYDATE,
                       b.STATUS, b.CANCELLED, b.DEPT_CODE, b.WORK_NAME, b.NASTINO, b.FILETYPE,
                       d.DEPT_NAME,
                       s.AC_SUBHEADNAMELL_UNICODE,
                       s.AC_SUBHEADNAMELL
                FROM ABASBUDGETBOOKENTRY b
                LEFT JOIN ULBERP.DEPARTMENTDET d
                       ON d.DEPT_CODE = b.DEPT_CODE AND d.ULB_CODE = b.ULB_CODE
                LEFT JOIN (
                       SELECT AC_SUBHEAD, ULB_CODE,
                              MAX(AC_SUBHEADNAMELL_UNICODE) AS AC_SUBHEADNAMELL_UNICODE,
                              MAX(AC_SUBHEADNAMELL) AS AC_SUBHEADNAMELL
                       FROM ABASSUBHEADDET
                       GROUP BY AC_SUBHEAD, ULB_CODE
                ) s ON s.AC_SUBHEAD = b.AC_SUBHEAD AND s.ULB_CODE = b.ULB_CODE
                WHERE b.ULB_CODE = :ulb_code AND b.BOOKENTRYNO = :book_entry_no";

            using (var conn = _connectionFactory.CreateAbas())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                cmd.Parameters.Add("book_entry_no", OracleDbType.Decimal).Value = bookEntryNo;
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        return MapEntry(reader);
                }
            }
            return null;
        }

        // ── Save final entry ─────────────────────────────────────────────────────

        public BudgetBookSaveResult SaveFinalEntry(int ulbCode, FinalBudgetEntryRequest request)
        {
            const string lockSql = @"
                SELECT BOOKENTRYNO, FIN_YEAR, AC_SUBHEAD, PROPOSEDWORK_AMOUNT,
                       STATUS, CANCELLED, BUDGET_AMOUNT
                FROM ABASBUDGETBOOKENTRY
                WHERE ULB_CODE = :ulb_code AND BOOKENTRYNO = :book_entry_no
                FOR UPDATE";

            // Committed = GAD quotations + GAD tenders + existing budget book entries (all non-cancelled)
            const string committedSql = @"
                SELECT
                    NVL((SELECT SUM(PROPOSEDCOST) FROM GAD.GADQUOTATIONORDERSDET
                         WHERE ULB_CODE = :ulb_code AND ACHEAD_CODE = :ac_subhead
                           AND FINYEAR = :fin_year AND VALIDFLAG = 'y'), 0)
                  + NVL((SELECT SUM(PROPOSEDCOST) FROM GAD.GADTENDERORDERSDET
                         WHERE ULB_CODE = :ulb_code AND ACHEAD_CODE = :ac_subhead
                           AND FINYEAR = :fin_year AND VALIDFLAG = 'y'), 0)
                  + NVL((SELECT SUM(PROPOSEDWORK_AMOUNT) FROM ABASBUDGETBOOKENTRY
                         WHERE ULB_CODE = :ulb_code AND AC_SUBHEAD = :ac_subhead
                           AND FIN_YEAR = :fin_year
                           AND (CANCELLED IS NULL OR UPPER(CANCELLED) <> 'Y')), 0)
                AS COMMITTED FROM DUAL";

            const string updateSql = @"
                UPDATE ABASBUDGETBOOKENTRY
                SET    FINAL_PROPOSEDWORK_AMOUNT = :final_amount,
                       FINALBOOKENTRYNO          = SEQFINALBOOKENTRY_NO.NEXTVAL,
                       FINALBUDGETENTBY          = :entered_by,
                       FINALBUDGETENTRYDATE      = SYSDATE,
                       STATUS                    = 'y'
                WHERE  ULB_CODE    = :ulb_code
                  AND  BOOKENTRYNO = :book_entry_no";

            using (var conn = _connectionFactory.CreateAbas())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        string finYear = null;
                        string acSubhead = null;
                        decimal primaryAmount = 0m;
                        decimal totalBudget = 0m;

                        // Lock the row and read it
                        using (var cmd = new OracleCommand(lockSql, conn))
                        {
                            cmd.Transaction = tx;
                            cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                            cmd.Parameters.Add("book_entry_no", OracleDbType.Decimal).Value = request.BookEntryNo;
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (!reader.Read())
                                {
                                    tx.Rollback();
                                    return new BudgetBookSaveResult
                                    {
                                        Success = false,
                                        Message = "नोंद सापडली नाही (BookEntryNo: " + request.BookEntryNo + ").",
                                    };
                                }
                                var status = reader["STATUS"] as string ?? string.Empty;
                                var cancelled = reader["CANCELLED"] as string ?? string.Empty;
                                if (status.Equals("y", StringComparison.OrdinalIgnoreCase))
                                {
                                    tx.Rollback();
                                    return new BudgetBookSaveResult
                                    {
                                        Success = false,
                                        Message = "या नोंदीची अंतिम नोंद आधीच झाली आहे.",
                                    };
                                }
                                if (cancelled.Equals("y", StringComparison.OrdinalIgnoreCase))
                                {
                                    tx.Rollback();
                                    return new BudgetBookSaveResult
                                    {
                                        Success = false,
                                        Message = "ही नोंद रद्द केलेली आहे.",
                                    };
                                }
                                finYear = reader["FIN_YEAR"] as string;
                                acSubhead = reader["AC_SUBHEAD"] as string;
                                primaryAmount = reader["PROPOSEDWORK_AMOUNT"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["PROPOSEDWORK_AMOUNT"]);
                                totalBudget = reader["BUDGET_AMOUNT"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["BUDGET_AMOUNT"]);
                            }
                        }

                        // Calculate remaining: totalBudget - ALL committed + this primary - this final
                        decimal allCommitted = 0m;
                        using (var cmd = new OracleCommand(committedSql, conn))
                        {
                            cmd.Transaction = tx;
                            cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                            cmd.Parameters.Add("ac_subhead", OracleDbType.Varchar2).Value = acSubhead;
                            cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = finYear;
                            var val = cmd.ExecuteScalar();
                            if (val != null && val != DBNull.Value)
                                allCommitted = Convert.ToDecimal(val);
                        }

                        // Remaining if we replace primary with final:
                        // (totalBudget - allCommitted) + primaryAmount - finalAmount
                        decimal currentRemaining = totalBudget - allCommitted;
                        decimal effectiveRemaining = currentRemaining + primaryAmount - request.FinalProposedAmount;

                        if (effectiveRemaining < 0)
                        {
                            tx.Rollback();
                            return new BudgetBookSaveResult
                            {
                                Success = false,
                                Message = string.Format(
                                    "अपुरा तरतूद: अंतिम किंमत ₹{0:N0} साठी तरतूद शिल्लक नाही. कमाल अनुज्ञेय रक्कम: ₹{1:N0}.",
                                    request.FinalProposedAmount,
                                    currentRemaining + primaryAmount),
                                RemainingBudget = currentRemaining + primaryAmount,
                            };
                        }

                        using (var cmd = new OracleCommand(updateSql, conn))
                        {
                            cmd.Transaction = tx;
                            cmd.Parameters.Add("final_amount", OracleDbType.Decimal).Value = request.FinalProposedAmount;
                            cmd.Parameters.Add("entered_by", OracleDbType.Varchar2).Value = TruncateString(request.EnteredBy, 8);
                            cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                            cmd.Parameters.Add("book_entry_no", OracleDbType.Decimal).Value = request.BookEntryNo;
                            cmd.ExecuteNonQuery();
                        }

                        // Read back the generated FINALBOOKENTRYNO
                        long finalBookEntryNo = 0;
                        using (var cmd = new OracleCommand(
                            "SELECT FINALBOOKENTRYNO FROM ABASBUDGETBOOKENTRY WHERE ULB_CODE = :ulb_code AND BOOKENTRYNO = :book_entry_no",
                            conn))
                        {
                            cmd.Transaction = tx;
                            cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                            cmd.Parameters.Add("book_entry_no", OracleDbType.Decimal).Value = request.BookEntryNo;
                            var val = cmd.ExecuteScalar();
                            if (val != null && val != DBNull.Value)
                                finalBookEntryNo = Convert.ToInt64(val);
                        }

                        tx.Commit();
                        return new BudgetBookSaveResult
                        {
                            Success = true,
                            Message = "अंतिम तरतूद नोंद यशस्वीरित्या जतन झाली.",
                            BookEntryNo = request.BookEntryNo,
                            FinalBookEntryNo = finalBookEntryNo,
                            RemainingBudget = effectiveRemaining,
                        };
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        return new BudgetBookSaveResult
                        {
                            Success = false,
                            Message = "त्रुटी: " + ex.Message,
                        };
                    }
                }
            }
        }

        // ── List entries ─────────────────────────────────────────────────────────

        public IEnumerable<BudgetBookEntryDto> ListEntries(int ulbCode, string finYear, int? deptCode, int pageNo, int pageSize, out int totalCount, string search = null, string status = null)
        {
            bool hasSearch = !string.IsNullOrWhiteSpace(search);
            string searchFilter = hasSearch
                ? @"AND (UPPER(b.WORK_NAME)  LIKE '%' || UPPER(:search) || '%'
                      OR TO_CHAR(b.NASTINO)  LIKE '%' || :search || '%'
                      OR UPPER(b.AC_SUBHEAD) LIKE '%' || UPPER(:search) || '%')"
                : string.Empty;

            // status: 'primary' = STATUS != 'Y', 'final' = STATUS = 'Y'
            string statusFilter = string.Empty;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (string.Equals(status.Trim(), "primary", StringComparison.OrdinalIgnoreCase))
                    statusFilter = "AND (b.STATUS IS NULL OR UPPER(b.STATUS) <> 'Y')";
                else if (string.Equals(status.Trim(), "final", StringComparison.OrdinalIgnoreCase))
                    statusFilter = "AND UPPER(b.STATUS) = 'Y'";
            }

            string deptFilter = deptCode.HasValue ? "AND b.DEPT_CODE = :dept_code " : "";

            string countSql = @"
                SELECT COUNT(*)
                FROM ABASBUDGETBOOKENTRY b
                WHERE b.ULB_CODE = :ulb_code
                  AND b.FIN_YEAR = :fin_year
                  AND (b.CANCELLED IS NULL OR UPPER(b.CANCELLED) <> 'Y')
                  " + deptFilter + searchFilter + " " + statusFilter;

            string sql = @"
                SELECT b.BOOKENTRYNO, b.FINALBOOKENTRYNO, b.FIN_YEAR, b.AC_HEAD, b.AC_SUBHEAD,
                       b.PROPOSEDWORK_AMOUNT, b.FINAL_PROPOSEDWORK_AMOUNT,
                       b.BUDGET_AMOUNT, b.REMAINING_BUDGET_AMOUNT,
                       b.BUDGETENTBY, b.BUDGETENTRYDATE,
                       b.FINALBUDGETENTBY, b.FINALBUDGETENTRYDATE,
                       b.STATUS, b.CANCELLED, b.DEPT_CODE, b.WORK_NAME, b.NASTINO, b.FILETYPE,
                       d.DEPT_NAME,
                       s.AC_SUBHEADNAMELL_UNICODE,
                       s.AC_SUBHEADNAMELL
                FROM ABASBUDGETBOOKENTRY b
                LEFT JOIN ULBERP.DEPARTMENTDET d
                       ON d.DEPT_CODE = b.DEPT_CODE AND d.ULB_CODE = b.ULB_CODE
                LEFT JOIN (
                       SELECT AC_SUBHEAD, ULB_CODE,
                              MAX(AC_SUBHEADNAMELL_UNICODE) AS AC_SUBHEADNAMELL_UNICODE,
                              MAX(AC_SUBHEADNAMELL) AS AC_SUBHEADNAMELL
                       FROM ABASSUBHEADDET
                       GROUP BY AC_SUBHEAD, ULB_CODE
                ) s ON s.AC_SUBHEAD = b.AC_SUBHEAD AND s.ULB_CODE = b.ULB_CODE
                WHERE b.ULB_CODE = :ulb_code
                  AND b.FIN_YEAR = :fin_year
                  AND (b.CANCELLED IS NULL OR UPPER(b.CANCELLED) <> 'Y')
                  " + deptFilter + searchFilter + " " + statusFilter + @"
                ORDER BY b.BOOKENTRYNO DESC
                OFFSET :offset ROWS FETCH NEXT :page_size ROWS ONLY";

            var result = new List<BudgetBookEntryDto>();
            using (var conn = _connectionFactory.CreateAbas())
            {
                conn.Open();
                // Count query
                using (var countCmd = new OracleCommand(countSql, conn))
                {
                    countCmd.BindByName = true;
                    countCmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                    countCmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = finYear;
                    if (deptCode.HasValue) countCmd.Parameters.Add("dept_code", OracleDbType.Decimal).Value = deptCode.Value;
                    if (hasSearch) countCmd.Parameters.Add("search", OracleDbType.Varchar2).Value = search.Trim();
                    totalCount = Convert.ToInt32(countCmd.ExecuteScalar() ?? 0);
                }
                // Data query
                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                    cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = finYear;
                    if (deptCode.HasValue) cmd.Parameters.Add("dept_code", OracleDbType.Decimal).Value = deptCode.Value;
                    if (hasSearch) cmd.Parameters.Add("search", OracleDbType.Varchar2).Value = search.Trim();
                    cmd.Parameters.Add("offset", OracleDbType.Decimal).Value = (pageNo - 1) * pageSize;
                    cmd.Parameters.Add("page_size", OracleDbType.Decimal).Value = pageSize;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            result.Add(MapEntry(reader));
                    }
                }
            }
            return result;
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static BudgetBookEntryDto MapEntry(IDataReader reader)
        {
            return new BudgetBookEntryDto
            {
                BookEntryNo = reader["BOOKENTRYNO"] == DBNull.Value ? 0L : Convert.ToInt64(reader["BOOKENTRYNO"]),
                FinalBookEntryNo = reader["FINALBOOKENTRYNO"] == DBNull.Value ? 0L : Convert.ToInt64(reader["FINALBOOKENTRYNO"]),
                FinYear = reader["FIN_YEAR"] as string ?? string.Empty,
                AcHead = reader["AC_HEAD"] as string ?? string.Empty,
                AcSubhead = reader["AC_SUBHEAD"] as string ?? string.Empty,
                AcSubheadName = SelectSubheadName(reader),
                ProposedWorkAmount = reader["PROPOSEDWORK_AMOUNT"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["PROPOSEDWORK_AMOUNT"]),
                FinalProposedWorkAmount = reader["FINAL_PROPOSEDWORK_AMOUNT"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["FINAL_PROPOSEDWORK_AMOUNT"]),
                BudgetAmount = reader["BUDGET_AMOUNT"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["BUDGET_AMOUNT"]),
                RemainingBudgetAmount = reader["REMAINING_BUDGET_AMOUNT"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["REMAINING_BUDGET_AMOUNT"]),
                EnteredBy = reader["BUDGETENTBY"] as string ?? string.Empty,
                EntryDate = reader["BUDGETENTRYDATE"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["BUDGETENTRYDATE"]),
                FinalEnteredBy = reader["FINALBUDGETENTBY"] as string,
                FinalEntryDate = reader["FINALBUDGETENTRYDATE"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["FINALBUDGETENTRYDATE"]),
                Status = reader["STATUS"] as string ?? "n",
                Cancelled = reader["CANCELLED"] as string ?? "n",
                DeptCode = reader["DEPT_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["DEPT_CODE"]),
                DeptName = reader["DEPT_NAME"] as string ?? string.Empty,
                WorkName = reader["WORK_NAME"] as string ?? string.Empty,
                NastiNo = reader["NASTINO"] == DBNull.Value ? 0L : Convert.ToInt64(reader["NASTINO"]),
                FileType = reader["FILETYPE"] as string ?? string.Empty,
            };
        }

        private static string SelectSubheadName(IDataReader reader)
        {
            // AC_SUBHEADNAMELL_UNICODE is NVARCHAR2 — Oracle.ManagedDataAccess returns it
            // as a proper Unicode string in .NET, avoiding WE8MSWIN1252 corruption.
            var unicode = reader["AC_SUBHEADNAMELL_UNICODE"] as string;
            if (!string.IsNullOrWhiteSpace(unicode))
                return unicode.Trim();
            return (reader["AC_SUBHEADNAMELL"] as string ?? string.Empty).Trim();
        }

        private static string TruncateString(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
