using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Oracle.ManagedDataAccess.Client;
using SmkcApi.Models.BudgetBook;

namespace SmkcApi.Repositories.BudgetBook
{
    public class BudgetBookRepository : IBudgetBookRepository
    {
        private const string UnmappedFundCode = "UNMAPPED";
        private const string UnmappedFundName = "Fund Not Mapped";

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

        public BudgetRemainingDto GetRemainingBudget(int ulbCode, string acSubhead, string finYear, long? excludeNastiNo = null)
        {
            const string budgetSql = @"
                SELECT NVL(SUM(DEBIT_AMOUNT + CREDIT_AMOUNT), 0) AS TOTAL_BUDGET
                FROM ABASDEPTBUDJETACTDET
                WHERE ULB_CODE = :ulb_code AND AC_SUBHEAD = :ac_subhead AND FIN_YEAR = :fin_year";

            // Committed = primary budget book entries only; exclude the current proposal's own entry to avoid double-counting
            string committedSql = @"
                SELECT NVL(SUM(PROPOSEDWORK_AMOUNT), 0) AS COMMITTED
                FROM ABASBUDGETBOOKENTRY
                WHERE ULB_CODE = :ulb_code AND AC_SUBHEAD = :ac_subhead
                  AND FIN_YEAR = :fin_year
                  AND (CANCELLED IS NULL OR UPPER(CANCELLED) <> 'Y')";
            if (excludeNastiNo.HasValue)
                committedSql += " AND (NASTINO IS NULL OR NASTINO <> :exclude_nasti_no)";

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
                    if (excludeNastiNo.HasValue)
                        cmd.Parameters.Add("exclude_nasti_no", OracleDbType.Decimal).Value = excludeNastiNo.Value;
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

            // Budget-book-only committed — used to compute REMAINING_BUDGET_AMOUNT for storage.
            // Matches GetRemainingBudget formula; avoids double-counting Q/T proposals whose
            // cost already appears in GADQUOTATIONORDERSDET / GADTENDERORDERSDET.
            const string committedStorageSql = @"
                SELECT NVL(SUM(PROPOSEDWORK_AMOUNT), 0) AS COMMITTED
                FROM ABASBUDGETBOOKENTRY
                WHERE ULB_CODE = :ulb_code AND AC_SUBHEAD = :ac_subhead
                  AND FIN_YEAR = :fin_year
                  AND (CANCELLED IS NULL OR UPPER(CANCELLED) <> 'Y')";

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
                     :entered_by, :entry_date,
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
                        decimal committedForStorage = 0m;

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

                        // BB-only committed — used for both validation and storage.
                        // Consistent with GetRemainingBudget; avoids double-counting GAD quotations/tenders
                        // which represent the same works being entered in the budget book.
                        using (var cmd = new OracleCommand(committedStorageSql, conn))
                        {
                            cmd.Transaction = tx;
                            cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                            cmd.Parameters.Add("ac_subhead", OracleDbType.Varchar2).Value = request.AcSubhead;
                            cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = request.FinYear;
                            var val = cmd.ExecuteScalar();
                            if (val != null && val != DBNull.Value)
                                committedForStorage = Convert.ToDecimal(val);
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

                        decimal remaining = effectiveBudget - committedForStorage;

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
                            cmd.Parameters.Add("remaining_budget", OracleDbType.Decimal).Value = effectiveBudget - committedForStorage - request.ProposedAmount;
                            cmd.Parameters.Add("entered_by", OracleDbType.Varchar2).Value = TruncateString(request.EnteredBy, 8);
                            cmd.Parameters.Add("entry_date", OracleDbType.Date).Value = DateTime.Now;
                            cmd.Parameters.Add("dept_code", OracleDbType.Decimal).Value = request.DeptCode;
                            cmd.Parameters.Add("work_name", OracleDbType.NVarchar2).Value = request.WorkName ?? string.Empty;
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
                            RemainingBudget = effectiveBudget - committedForStorage - request.ProposedAmount,
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

            // Committed = existing budget book entries only (BB-only, same as SavePrimaryEntry)
            const string committedSql = @"
                SELECT NVL(SUM(PROPOSEDWORK_AMOUNT), 0) AS COMMITTED
                FROM ABASBUDGETBOOKENTRY
                WHERE ULB_CODE = :ulb_code AND AC_SUBHEAD = :ac_subhead
                  AND FIN_YEAR = :fin_year
                  AND (CANCELLED IS NULL OR UPPER(CANCELLED) <> 'Y')";

            const string updateSql = @"
                UPDATE ABASBUDGETBOOKENTRY
                SET    FINAL_PROPOSEDWORK_AMOUNT = :final_amount,
                       FINALBOOKENTRYNO          = SEQFINALBOOKENTRY_NO.NEXTVAL,
                       FINALBUDGETENTBY          = :entered_by,
                       FINALBUDGETENTRYDATE      = :final_entry_date,
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
                            cmd.Parameters.Add("final_entry_date", OracleDbType.Date).Value = DateTime.Now;
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

        // ── Budget entry liability report ──────────────────────────────────────

        public BudgetLiabilityReportResult GetLiabilityReport(int ulbCode, DateTime fromDate, DateTime toDate)
        {
            var rows = LoadLiabilityReportRows(ulbCode, fromDate, toDate);
            var result = new BudgetLiabilityReportResult
            {
                FromDate = fromDate.Date,
                ToDate = toDate.Date,
                Rows = rows,
                GrandTotalBudget = rows.Sum(row => row.TotalBudgetAmount),
                GrandTotalExpenditure = rows.Sum(row => row.TotalExpenditureAmount),
                GrandTotalBalance = rows.Sum(row => row.BudgetBalanceAmount),
            };

            return result;
        }

        public FundWiseBudgetLiabilityReportResult GetFundWiseLiabilityReport(int ulbCode, DateTime fromDate, DateTime toDate)
        {
            var baseReport = GetLiabilityReport(ulbCode, fromDate, toDate);
            var fundMappings = GetSubheadFundMappings(ulbCode);
            var fundGroups = new List<FundWiseBudgetLiabilityReportFundDto>();

            foreach (var group in baseReport.Rows
                .GroupBy(row =>
                {
                    SubheadFundMapping mapping;
                    if (!fundMappings.TryGetValue(row.AcSubhead, out mapping) || mapping == null)
                    {
                        return UnmappedFundCode + "|" + UnmappedFundName;
                    }

                    return mapping.FundCode + "|" + mapping.FundName;
                }))
            {
                var sample = group.First();
                SubheadFundMapping mapping;
                var hasMapping = fundMappings.TryGetValue(sample.AcSubhead, out mapping) && mapping != null;
                var orderedDetails = group
                    .OrderByDescending(row => row.FinYear)
                    .ThenBy(row => row.AcSubhead)
                    .ToList();

                fundGroups.Add(new FundWiseBudgetLiabilityReportFundDto
                {
                    FundCode = hasMapping ? mapping.FundCode : UnmappedFundCode,
                    FundName = hasMapping ? mapping.FundName : UnmappedFundName,
                    TotalBudgetAmount = orderedDetails.Sum(row => row.TotalBudgetAmount),
                    TotalExpenditureAmount = orderedDetails.Sum(row => row.TotalExpenditureAmount),
                    BudgetBalanceAmount = orderedDetails.Sum(row => row.BudgetBalanceAmount),
                    Details = orderedDetails,
                });
            }

            return new FundWiseBudgetLiabilityReportResult
            {
                FromDate = baseReport.FromDate,
                ToDate = baseReport.ToDate,
                Funds = fundGroups
                    .OrderBy(group => group.FundCode == UnmappedFundCode ? 1 : 0)
                    .ThenBy(group => group.FundCode)
                    .ThenBy(group => group.FundName)
                    .ToList(),
                GrandTotalBudget = baseReport.GrandTotalBudget,
                GrandTotalExpenditure = baseReport.GrandTotalExpenditure,
                GrandTotalBalance = baseReport.GrandTotalBalance,
                UnmappedBudgetHeadCount = baseReport.Rows.Count(row =>
                {
                    SubheadFundMapping mapping;
                    if (!fundMappings.TryGetValue(row.AcSubhead, out mapping) || mapping == null)
                    {
                        return true;
                    }

                    return string.IsNullOrWhiteSpace(mapping.FundCode) ||
                           string.Equals(mapping.FundCode, UnmappedFundCode, StringComparison.OrdinalIgnoreCase);
                }),
            };
        }

        private List<BudgetLiabilityReportRowDto> LoadLiabilityReportRows(int ulbCode, DateTime fromDate, DateTime toDate)
        {
            const string sql = @"
                WITH entry_agg AS (
                    SELECT b.FIN_YEAR,
                           b.AC_SUBHEAD,
                           SUM(
                               CASE
                                   WHEN UPPER(NVL(b.STATUS, 'N')) = 'Y' AND NVL(b.FINAL_PROPOSEDWORK_AMOUNT, 0) > 0
                                       THEN NVL(b.FINAL_PROPOSEDWORK_AMOUNT, 0)
                                   ELSE NVL(b.PROPOSEDWORK_AMOUNT, 0)
                               END
                           ) AS TOTAL_EXPENDITURE
                    FROM ABASBUDGETBOOKENTRY b
                    WHERE b.ULB_CODE = :ulb_code
                      AND (b.CANCELLED IS NULL OR UPPER(b.CANCELLED) <> 'Y')
                      AND TRUNC(b.BUDGETENTRYDATE) BETWEEN TRUNC(:from_date) AND TRUNC(:to_date)
                    GROUP BY b.FIN_YEAR, b.AC_SUBHEAD
                ),
                budget_agg AS (
                    SELECT d.FIN_YEAR,
                           d.AC_SUBHEAD,
                           NVL(SUM(NVL(d.DEBIT_AMOUNT, 0) + NVL(d.CREDIT_AMOUNT, 0)), 0) AS TOTAL_BUDGET
                    FROM ABASDEPTBUDJETACTDET d
                    WHERE d.ULB_CODE = :ulb_code
                    GROUP BY d.FIN_YEAR, d.AC_SUBHEAD
                ),
                subhead_names AS (
                    SELECT s.ULB_CODE,
                           s.AC_SUBHEAD,
                           MAX(s.AC_SUBHEADNAMELL_UNICODE) AS AC_SUBHEADNAMELL_UNICODE,
                           MAX(s.AC_SUBHEADNAMELL) AS AC_SUBHEADNAMELL
                    FROM ABASSUBHEADDET s
                    GROUP BY s.ULB_CODE, s.AC_SUBHEAD
                )
                SELECT e.FIN_YEAR,
                       e.AC_SUBHEAD,
                       NVL(n.AC_SUBHEADNAMELL_UNICODE, n.AC_SUBHEADNAMELL) AS AC_SUBHEAD_NAME,
                       NVL(b.TOTAL_BUDGET, 0) AS TOTAL_BUDGET,
                       NVL(e.TOTAL_EXPENDITURE, 0) AS TOTAL_EXPENDITURE,
                       NVL(b.TOTAL_BUDGET, 0) - NVL(e.TOTAL_EXPENDITURE, 0) AS BALANCE
                FROM entry_agg e
                LEFT JOIN budget_agg b
                       ON b.FIN_YEAR = e.FIN_YEAR
                      AND b.AC_SUBHEAD = e.AC_SUBHEAD
                LEFT JOIN subhead_names n
                       ON n.ULB_CODE = :ulb_code
                      AND n.AC_SUBHEAD = e.AC_SUBHEAD
                ORDER BY e.FIN_YEAR DESC, e.AC_SUBHEAD";

            var rows = new List<BudgetLiabilityReportRowDto>();

            using (var conn = _connectionFactory.CreateAbas())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                cmd.Parameters.Add("from_date", OracleDbType.Date).Value = fromDate.Date;
                cmd.Parameters.Add("to_date", OracleDbType.Date).Value = toDate.Date;

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var row = new BudgetLiabilityReportRowDto
                        {
                            FinYear = reader["FIN_YEAR"] as string ?? string.Empty,
                            AcSubhead = reader["AC_SUBHEAD"] as string ?? string.Empty,
                            AcSubheadName = (reader["AC_SUBHEAD_NAME"] as string ?? string.Empty).Trim(),
                            TotalBudgetAmount = reader["TOTAL_BUDGET"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["TOTAL_BUDGET"]),
                            TotalExpenditureAmount = reader["TOTAL_EXPENDITURE"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["TOTAL_EXPENDITURE"]),
                            BudgetBalanceAmount = reader["BALANCE"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["BALANCE"]),
                        };
                        rows.Add(row);
                    }
                }
            }

            return rows;
        }

        private Dictionary<string, SubheadFundMapping> GetSubheadFundMappings(int ulbCode)
        {
            const string sql = @"
              WITH base_heads AS (
                  SELECT s.AC_SUBHEAD
                  FROM ABAS.ABASSUBHEADDET s
                  WHERE s.ULB_CODE = :ulb_code
                  UNION
                  SELECT m.AC_SUBHEAD
                  FROM ABAS.BUDGET_FUND_MAPPING m
                  WHERE m.ULB_CODE = :ulb_code
                 AND NVL(m.IS_ACTIVE, 'Y') = 'Y'
              ),
              manual_map AS (
                  SELECT m.AC_SUBHEAD,
                      MAX(m.FUND_CODE) AS FUND_CODE
                  FROM ABAS.BUDGET_FUND_MAPPING m
                  WHERE m.ULB_CODE = :ulb_code
                 AND NVL(m.IS_ACTIVE, 'Y') = 'Y'
                  GROUP BY m.AC_SUBHEAD
              ),
              subhead_map AS (
                  SELECT s.AC_SUBHEAD,
                      MAX(s.FUND_CODE) AS FUND_CODE
                  FROM ABAS.ABASSUBHEADDET s
                  WHERE s.ULB_CODE = :ulb_code
                  GROUP BY s.AC_SUBHEAD
              )
              SELECT h.AC_SUBHEAD,
                  NVL(mm.FUND_CODE, sm.FUND_CODE) AS FUND_CODE,
                  NVL(f.FUND_DESC, :unmapped_fund_name) AS FUND_NAME
              FROM base_heads h
              LEFT JOIN manual_map mm
                  ON mm.AC_SUBHEAD = h.AC_SUBHEAD
              LEFT JOIN subhead_map sm
                  ON sm.AC_SUBHEAD = h.AC_SUBHEAD
              LEFT JOIN ABAS.ABASFUNDDET f
                  ON f.ULB_CODE = :ulb_code
                 AND f.FUND_CODE = NVL(mm.FUND_CODE, sm.FUND_CODE)
                 AND NVL(f.VALIDFLAG, 'y') = 'y'
                 AND NVL(f.STATUS, 'y') = 'y'";

            var mappings = new Dictionary<string, SubheadFundMapping>(StringComparer.OrdinalIgnoreCase);

            using (var conn = _connectionFactory.CreateAbas())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("unmapped_fund_name", OracleDbType.Varchar2).Value = UnmappedFundName;
                cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var acSubhead = reader["AC_SUBHEAD"] as string;
                        if (string.IsNullOrWhiteSpace(acSubhead))
                        {
                            continue;
                        }

                        mappings[acSubhead] = new SubheadFundMapping
                        {
                            FundCode = reader["FUND_CODE"] == DBNull.Value ? UnmappedFundCode : Convert.ToString(reader["FUND_CODE"]),
                            FundName = (reader["FUND_NAME"] as string ?? UnmappedFundName).Trim(),
                        };
                    }
                }
            }

            return mappings;
        }

        // ── Bill payment report ────────────────────────────────────────────────

        public BillPaymentReportResult GetBillPaymentReport(int ulbCode, DateTime fromDate, DateTime toDate)
        {
            const string sql = @"
                WITH bill_base AS (
                    SELECT t.BUDGETCODE AS AC_SUBHEAD,
                           GETFINYEAR(t.AUTHDT) AS FIN_YEAR,
                           t.REGBILLNO,
                           SUM(CASE WHEN b.ITEMTYPE = '2'
                                    THEN NVL(b.RATE, 0) * NVL(b.QUANTITY, 0)
                                    ELSE 0 END) AS AMOUNT2,
                           SUM(CASE WHEN b.ITEMTYPE = '1'
                                    THEN CASE
                                         WHEN t.ABOVEBELOWTYPE = '2'
                                         THEN (NVL(b.RATE, 0) * NVL(b.QUANTITY, 0)) -
                                              (((NVL(b.RATE, 0) * NVL(b.QUANTITY, 0)) * NVL(t.CONTRACTORPER, 0)) / 100)
                                         ELSE (NVL(b.RATE, 0) * NVL(b.QUANTITY, 0)) +
                                              (((NVL(b.RATE, 0) * NVL(b.QUANTITY, 0)) * NVL(t.CONTRACTORPER, 0)) / 100)
                                         END
                                    ELSE 0 END) AS ABOVE_BELOW_AMOUNT
                    FROM ABAS.OBREGULARBILLDET t
                    JOIN ABAS.OBREGULARBILLTRANDET b
                         ON b.ULB_CODE = t.ULB_CODE
                        AND b.REGBILLNO = t.REGBILLNO
                    JOIN ABAS.OBBILLFLOWDET w
                         ON w.ULB_CODE = t.ULB_CODE
                        AND w.DEPTBILLNO = t.DEPTBILLNO
                    WHERE t.ULB_CODE = :ulb_code
                      AND TRUNC(t.AUTHDT) BETWEEN TRUNC(:from_date) AND TRUNC(:to_date)
                      AND NVL(t.VALIDFLAG, 'y') = 'y'
                      AND w.BILLSTAGE = 5
                    GROUP BY t.BUDGETCODE, GETFINYEAR(t.AUTHDT), t.REGBILLNO
                ),
                bill_ded AS (
                    SELECT t.REGBILLNO,
                           SUM(NVL(d.AMOUNT, 0)) AS DED_AMOUNT
                    FROM ABAS.OBREGULARBILLDET t
                    JOIN ABAS.OBBILLDEDUCTIONDET d
                         ON d.ULB_CODE = t.ULB_CODE
                        AND d.REGULARBILLNO = t.REGBILLNO
                    WHERE t.ULB_CODE = :ulb_code
                      AND TRUNC(t.AUTHDT) BETWEEN TRUNC(:from_date) AND TRUNC(:to_date)
                      AND NVL(t.VALIDFLAG, 'y') = 'y'
                    GROUP BY t.REGBILLNO
                ),
                expenditure_agg AS (
                    SELECT b.AC_SUBHEAD,
                           b.FIN_YEAR,
                           SUM(NVL(b.AMOUNT2, 0) + NVL(b.ABOVE_BELOW_AMOUNT, 0) - NVL(d.DED_AMOUNT, 0)) AS TOTAL_EXPENDITURE
                    FROM bill_base b
                    LEFT JOIN bill_ded d
                           ON d.REGBILLNO = b.REGBILLNO
                    GROUP BY b.AC_SUBHEAD, b.FIN_YEAR
                ),
                budget_agg AS (
                    SELECT d.AC_SUBHEAD,
                           d.FIN_YEAR,
                           NVL(SUM(NVL(d.DEBIT_AMOUNT, 0) + NVL(d.CREDIT_AMOUNT, 0)), 0) AS TOTAL_BUDGET
                    FROM ABAS.ABASDEPTBUDJETACTDET d
                    WHERE d.ULB_CODE = :ulb_code
                    GROUP BY d.AC_SUBHEAD, d.FIN_YEAR
                ),
                subhead_names AS (
                    SELECT s.ULB_CODE,
                           s.AC_SUBHEAD,
                           MAX(s.AC_SUBHEADNAMELL_UNICODE) AS AC_SUBHEADNAMELL_UNICODE,
                           MAX(s.AC_SUBHEADNAMELL) AS AC_SUBHEADNAMELL
                    FROM ABAS.ABASSUBHEADDET s
                    WHERE s.ULB_CODE = :ulb_code
                    GROUP BY s.ULB_CODE, s.AC_SUBHEAD
                )
                SELECT e.FIN_YEAR,
                       e.AC_SUBHEAD,
                       NVL(n.AC_SUBHEADNAMELL_UNICODE, n.AC_SUBHEADNAMELL) AS AC_SUBHEAD_NAME,
                       NVL(b.TOTAL_BUDGET, 0) AS TOTAL_BUDGET,
                       NVL(e.TOTAL_EXPENDITURE, 0) AS TOTAL_EXPENDITURE,
                       NVL(b.TOTAL_BUDGET, 0) - NVL(e.TOTAL_EXPENDITURE, 0) AS BALANCE
                FROM expenditure_agg e
                LEFT JOIN budget_agg b
                       ON b.AC_SUBHEAD = e.AC_SUBHEAD
                      AND b.FIN_YEAR = e.FIN_YEAR
                LEFT JOIN subhead_names n
                       ON n.ULB_CODE = :ulb_code
                      AND n.AC_SUBHEAD = e.AC_SUBHEAD
                ORDER BY e.FIN_YEAR DESC, e.AC_SUBHEAD";

            var result = new BillPaymentReportResult
            {
                FromDate = fromDate.Date,
                ToDate = toDate.Date,
                Rows = new List<BillPaymentReportRowDto>(),
            };

            using (var conn = _connectionFactory.CreateAbas())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                cmd.Parameters.Add("from_date", OracleDbType.Date).Value = fromDate.Date;
                cmd.Parameters.Add("to_date", OracleDbType.Date).Value = toDate.Date;

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var row = new BillPaymentReportRowDto
                        {
                            FinYear = reader["FIN_YEAR"] as string ?? string.Empty,
                            AcSubhead = reader["AC_SUBHEAD"] as string ?? string.Empty,
                            AcSubheadName = (reader["AC_SUBHEAD_NAME"] as string ?? string.Empty).Trim(),
                            TotalBudgetAmount = reader["TOTAL_BUDGET"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["TOTAL_BUDGET"]),
                            TotalExpenditureAmount = reader["TOTAL_EXPENDITURE"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["TOTAL_EXPENDITURE"]),
                            BudgetBalanceAmount = reader["BALANCE"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["BALANCE"]),
                        };
                        result.GrandTotalBudget += row.TotalBudgetAmount;
                        result.GrandTotalExpenditure += row.TotalExpenditureAmount;
                        result.GrandTotalBalance += row.BudgetBalanceAmount;
                        result.Rows.Add(row);
                    }
                }
            }
            return result;
        }

        public WardWiseExpenditureReportResult GetWardWiseExpenditureReport(
            int ulbCode,
            DateTime fromDate,
            DateTime toDate,
            string finYear = null,
            string wardNo = null,
            string wardScope = null,
            string deptName = null)
        {
            var effectiveFinYear = string.IsNullOrWhiteSpace(finYear)
                ? string.Format("{0}-{1}", fromDate.Year, fromDate.Year + 1)
                : finYear.Trim();
            var effectiveWardNo = string.IsNullOrWhiteSpace(wardNo) ? null : wardNo.Trim();
            var effectiveDeptName = string.IsNullOrWhiteSpace(deptName) ? null : deptName.Trim();

            var effectiveWardScope = string.IsNullOrWhiteSpace(wardScope)
                ? (string.IsNullOrWhiteSpace(effectiveWardNo) ? "ALL" : "SINGLE")
                : wardScope.Trim().ToUpperInvariant();

            if (effectiveWardScope != "ALL" && effectiveWardScope != "SINGLE" && effectiveWardScope != "MULTI" && effectiveWardScope != "UNMAPPED")
                effectiveWardScope = string.IsNullOrWhiteSpace(effectiveWardNo) ? "ALL" : "SINGLE";

            const string sql = @"
                WITH proposal_union AS (
                    SELECT q.FINYEAR,
                           q.DEPT_CODE,
                           q.NASTINO,
                           q.PROPOSALNAME AS WORK_NAME,
                           q.WARDNO,
                           NVL(q.PROPOSEDCOST, 0) AS PROPOSEDCOST,
                           q.ACHEAD_CODE,
                          q.ENTDT,
                          'Q' AS FILETYPE
                    FROM GAD.GADQUOTATIONORDERSDET q
                    WHERE q.ULB_CODE = :ulb_code
                      AND NVL(q.VALIDFLAG, 'y') = 'y'
                      AND q.FINYEAR = :fin_year
                      AND TRUNC(NVL(q.ENTDT, SYSDATE)) BETWEEN TRUNC(:from_date) AND TRUNC(:to_date)

                    UNION ALL

                    SELECT t.FINYEAR,
                           t.DEPT_CODE,
                           t.NASTINO,
                           t.PROPOSALNAME AS WORK_NAME,
                           t.WARDNO,
                           NVL(t.PROPOSEDCOST, 0) AS PROPOSEDCOST,
                           t.ACHEAD_CODE,
                              t.ENTDT,
                              'T' AS FILETYPE
                    FROM GAD.GADTENDERORDERSDET t
                    WHERE t.ULB_CODE = :ulb_code
                      AND NVL(t.VALIDFLAG, 'y') = 'y'
                      AND t.FINYEAR = :fin_year
                      AND TRUNC(NVL(t.ENTDT, SYSDATE)) BETWEEN TRUNC(:from_date) AND TRUNC(:to_date)
                      ),
                      bb_entries AS (
                          SELECT e.FIN_YEAR,
                                 e.DEPT_CODE,
                                 e.NASTINO,
                                 NVL(e.BOOKENTRYNO, 0) AS BOOKENTRYNO,
                                 NVL(e.FINALBOOKENTRYNO, 0) AS FINALBOOKENTRYNO,
                                 NVL(e.PROPOSEDWORK_AMOUNT, 0) AS PROPOSEDWORK_AMOUNT,
                                 NVL(e.FINAL_PROPOSEDWORK_AMOUNT, 0) AS FINAL_PROPOSEDWORK_AMOUNT
                          FROM (
                              SELECT b.FIN_YEAR,
                                     b.DEPT_CODE,
                                     b.NASTINO,
                                     b.BOOKENTRYNO,
                                     b.FINALBOOKENTRYNO,
                                     b.PROPOSEDWORK_AMOUNT,
                                     b.FINAL_PROPOSEDWORK_AMOUNT,
                                     ROW_NUMBER() OVER (
                                         PARTITION BY b.FIN_YEAR, b.DEPT_CODE, b.NASTINO
                                         ORDER BY CASE WHEN NVL(b.FINALBOOKENTRYNO, 0) > 0 THEN 1 ELSE 0 END DESC,
                                                  NVL(b.FINALBUDGETENTRYDATE, NVL(b.BUDGETENTRYDATE, DATE '1900-01-01')) DESC,
                                                  NVL(b.FINALBOOKENTRYNO, 0) DESC,
                                                  NVL(b.BOOKENTRYNO, 0) DESC
                                     ) AS RN
                              FROM ABASBUDGETBOOKENTRY b
                              WHERE b.ULB_CODE = :ulb_code
                                AND b.FIN_YEAR = :fin_year
                                AND (b.CANCELLED IS NULL OR UPPER(b.CANCELLED) <> 'Y')
                                AND (NVL(b.BOOKENTRYNO, 0) > 0 OR NVL(b.FINALBOOKENTRYNO, 0) > 0)
                          ) e
                          WHERE e.RN = 1
                )
                  , scoped_rows AS (
                      SELECT p.FINYEAR,
                          p.DEPT_CODE,
                          NVL(d.DEPT_NAMELL_UNICODE, NVL(d.DEPT_NAMELL, d.DEPT_NAME)) AS DEPT_NAME,
                          p.NASTINO,
                          bbe.BOOKENTRYNO,
                          bbe.FINALBOOKENTRYNO,
                          p.WORK_NAME,
                          p.WARDNO,
                          CASE
                              WHEN NVL(bbe.FINALBOOKENTRYNO, 0) > 0 THEN NVL(bbe.FINAL_PROPOSEDWORK_AMOUNT, 0)
                              ELSE NVL(bbe.PROPOSEDWORK_AMOUNT, NVL(p.PROPOSEDCOST, 0))
                          END AS PROPOSEDCOST,
                          p.ACHEAD_CODE,
                          p.ENTDT,
                          NVL(s.AC_SUBHEADNAMELL_UNICODE, NVL(s.AC_SUBHEADNAMELL, s.AC_SUBHEADNAME)) AS AC_SUBHEAD_NAME
                      FROM proposal_union p
                      JOIN bb_entries bbe
                     ON bbe.FIN_YEAR = p.FINYEAR
                    AND bbe.DEPT_CODE = p.DEPT_CODE
                    AND bbe.NASTINO = p.NASTINO
                      LEFT JOIN ULBERP.DEPARTMENTDET d
                     ON d.ULB_CODE = :ulb_code
                    AND d.DEPT_CODE = p.DEPT_CODE
                      LEFT JOIN ABAS.ABASSUBHEADDET s
                     ON s.ULB_CODE = :ulb_code
                    AND s.AC_SUBHEAD = p.ACHEAD_CODE
                      WHERE (
                        :ward_scope = 'ALL'
                        OR (
                            :ward_scope = 'SINGLE'
                        AND REGEXP_LIKE(REPLACE(NVL(p.WARDNO, ''), ' ', ''), '^[0-9]+$')
                        AND TO_NUMBER(REPLACE(NVL(p.WARDNO, ''), ' ', '')) BETWEEN 1 AND 20
                        AND REPLACE(NVL(p.WARDNO, ''), ' ', '') = :ward_no
                        )
                        OR (
                            :ward_scope = 'MULTI'
                        AND (
                            REGEXP_LIKE(REPLACE(NVL(p.WARDNO, ''), ' ', ''), '^[0-9]+(,[0-9]+)+$')
                            OR REGEXP_LIKE(REPLACE(NVL(p.WARDNO, ''), ' ', ''), '^[0-9]+-[0-9]+$')
                            OR REGEXP_LIKE(REPLACE(UPPER(NVL(p.WARDNO, '')), ' ', ''), '^(ALL|ALLWARD|ALLWARDS)$')
                        )
                        )
                        OR (
                            :ward_scope = 'UNMAPPED'
                        AND NOT (
                            (REGEXP_LIKE(REPLACE(NVL(p.WARDNO, ''), ' ', ''), '^[0-9]+$')
                             AND TO_NUMBER(REPLACE(NVL(p.WARDNO, ''), ' ', '')) BETWEEN 1 AND 20)
                            OR REGEXP_LIKE(REPLACE(NVL(p.WARDNO, ''), ' ', ''), '^[0-9]+(,[0-9]+)+$')
                            OR REGEXP_LIKE(REPLACE(NVL(p.WARDNO, ''), ' ', ''), '^[0-9]+-[0-9]+$')
                            OR REGEXP_LIKE(REPLACE(UPPER(NVL(p.WARDNO, '')), ' ', ''), '^(ALL|ALLWARD|ALLWARDS)$')
                        )
                        )
                         )
                     AND (
                        :dept_name IS NULL
                        OR UPPER(NVL(d.DEPT_NAMELL_UNICODE, NVL(d.DEPT_NAMELL, d.DEPT_NAME))) LIKE '%' || UPPER(:dept_name) || '%'
                         )
                  ), ranked_rows AS (
                      SELECT sr.*,
                          ROW_NUMBER() OVER (
                           PARTITION BY sr.FINYEAR,
                                  sr.DEPT_CODE,
                                  sr.NASTINO,
                                  NVL(sr.BOOKENTRYNO, 0),
                                  NVL(sr.FINALBOOKENTRYNO, 0)
                           ORDER BY NVL(sr.ENTDT, DATE '1900-01-01') DESC,
                                 NVL(sr.PROPOSEDCOST, 0) DESC,
                                 sr.WORK_NAME,
                                 sr.ACHEAD_CODE
                          ) AS RN
                      FROM scoped_rows sr
                  )
                  SELECT rr.FINYEAR,
                      rr.DEPT_NAME,
                      rr.NASTINO,
                      rr.BOOKENTRYNO,
                      rr.FINALBOOKENTRYNO,
                      rr.WORK_NAME,
                      rr.WARDNO,
                      rr.PROPOSEDCOST,
                      rr.ACHEAD_CODE,
                      rr.AC_SUBHEAD_NAME
                  FROM ranked_rows rr
                  WHERE rr.RN = 1
                  ORDER BY TO_NUMBER(REGEXP_SUBSTR(NVL(rr.WARDNO, '0'), '\d+')) NULLS LAST,
                        rr.DEPT_NAME,
                        rr.NASTINO";

            var result = new WardWiseExpenditureReportResult
            {
                FromDate = fromDate.Date,
                ToDate = toDate.Date,
                FinYear = effectiveFinYear,
                WardNo = effectiveWardNo,
                WardScope = effectiveWardScope,
                DeptNameFilter = effectiveDeptName,
                Rows = new List<WardWiseExpenditureReportRowDto>(),
            };

            var fundMappings = GetSubheadFundMappings(ulbCode);

            using (var conn = _connectionFactory.CreateAbas())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = effectiveFinYear;
                cmd.Parameters.Add("from_date", OracleDbType.Date).Value = fromDate.Date;
                cmd.Parameters.Add("to_date", OracleDbType.Date).Value = toDate.Date;
                cmd.Parameters.Add("ward_no", OracleDbType.Varchar2).Value = string.IsNullOrWhiteSpace(effectiveWardNo)
                    ? (object)DBNull.Value
                    : effectiveWardNo;
                cmd.Parameters.Add("ward_scope", OracleDbType.Varchar2).Value = effectiveWardScope;
                cmd.Parameters.Add("dept_name", OracleDbType.Varchar2).Value = string.IsNullOrWhiteSpace(effectiveDeptName)
                    ? (object)DBNull.Value
                    : effectiveDeptName;

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var budgetHeadCode = reader["ACHEAD_CODE"] as string ?? string.Empty;
                        SubheadFundMapping fundMapping;
                        var fundName = fundMappings.TryGetValue(budgetHeadCode, out fundMapping) && fundMapping != null
                            ? (fundMapping.FundName ?? UnmappedFundName)
                            : UnmappedFundName;

                        var row = new WardWiseExpenditureReportRowDto
                        {
                            FinYear = reader["FINYEAR"] as string ?? string.Empty,
                            DeptName = (reader["DEPT_NAME"] as string ?? string.Empty).Trim(),
                            NastiNo = reader["NASTINO"] == DBNull.Value ? 0L : Convert.ToInt64(reader["NASTINO"]),
                            BookEntryNo = reader["BOOKENTRYNO"] == DBNull.Value ? 0L : Convert.ToInt64(reader["BOOKENTRYNO"]),
                            FinalBookEntryNo = reader["FINALBOOKENTRYNO"] == DBNull.Value ? 0L : Convert.ToInt64(reader["FINALBOOKENTRYNO"]),
                            WorkName = (reader["WORK_NAME"] as string ?? string.Empty).Trim(),
                            WardNo = (reader["WARDNO"] as string ?? string.Empty).Trim(),
                            ProposedCost = reader["PROPOSEDCOST"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["PROPOSEDCOST"]),
                            BudgetHeadCode = budgetHeadCode,
                            BudgetHeadName = (reader["AC_SUBHEAD_NAME"] as string ?? string.Empty).Trim(),
                            FundName = fundName,
                        };

                        result.TotalProposedCost += row.ProposedCost;
                        result.Rows.Add(row);
                    }
                }
            }

            return result;
        }

        private sealed class SubheadFundMapping
        {
            public string FundCode { get; set; }
            public string FundName { get; set; }
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
