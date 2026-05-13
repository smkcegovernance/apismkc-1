using System;
using System.Collections.Generic;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using SmkcApi.Models.BudgetBook;
using SmkcApi.Models.GAD;

namespace SmkcApi.Repositories.GAD
{
    public class GadWorkProposalRepository : IGadWorkProposalRepository
    {
        private readonly IOracleConnectionFactory _connectionFactory;

        public GadWorkProposalRepository(IOracleConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException("connectionFactory");
        }

        // ── Departments ──────────────────────────────────────────────────────────

        public IEnumerable<DepartmentDto> GetDepartments(int ulbCode, string userId)
        {
            bool isTestUser = string.IsNullOrWhiteSpace(userId) ||
                              string.Equals(userId.Trim(), "PTTEST01", StringComparison.OrdinalIgnoreCase);

            // For regular users, filter to their own department via USERDET
            const string allSql = @"
                SELECT DEPT_CODE, DEPT_NAME, DEPT_NAMELL, DEPT_NAMELL_UNICODE
                FROM ULBERP.DEPARTMENTDET
                WHERE ULB_CODE = :ulb_code AND VALIDFLAG = 'y' AND STATUS = 'y'
                ORDER BY DEPT_NAMELL_UNICODE";

            const string userSql = @"
                SELECT d.DEPT_CODE, d.DEPT_NAME, d.DEPT_NAMELL, d.DEPT_NAMELL_UNICODE
                FROM ULBERP.DEPARTMENTDET d
                JOIN ULBERP.USERDET u
                  ON d.DEPT_CODE = u.DEPT_CODE AND d.ULB_CODE = u.ULB_CODE
                WHERE d.ULB_CODE = :ulb_code
                  AND d.VALIDFLAG = 'y'
                  AND d.STATUS    = 'y'
                  AND u.USER_ID   = :user_id
                ORDER BY d.DEPT_NAMELL_UNICODE";

            var result = new List<DepartmentDto>();
            using (var conn = _connectionFactory.CreateAbas())
            {
                var sql = isTestUser ? allSql : userSql;
                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                    if (!isTestUser)
                        cmd.Parameters.Add("user_id", OracleDbType.Varchar2).Value = userId.Trim().ToUpper();
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
            }
            return result;
        }

        // ── Account Heads for GAD (E-% filtered by dept) ─────────────────────────

        public IEnumerable<GadSubheadDto> GetAccountHeads(int ulbCode, int deptCode, string finYear)
        {
            // Returns expense (E-%) account subheads that have a budget entry
            // for the given department and financial year.
            const string sql = @"
                SELECT DISTINCT d.AC_SUBHEAD,
                       d.AC_SUBHEADNAME,
                       d.AC_SUBHEADNAMELL,
                       d.AC_SUBHEADNAMELL_UNICODE
                FROM ABASDEPTBUDJETACTDET a
                JOIN ABASSUBHEADDET d ON a.AC_SUBHEAD = d.AC_SUBHEAD
                WHERE d.AC_SUBHEAD LIKE 'E-%'
                  AND a.STATUS    = 'y'
                  AND d.VALIDFLAG = 'y'
                  AND a.FIN_YEAR  = :fin_year
                  AND (a.DEPT_CODE = :dept_code OR a.DEPT_CODE = 0)
                ORDER BY d.AC_SUBHEAD";

            var result = new List<GadSubheadDto>();
            using (var conn = _connectionFactory.CreateAbas())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = finYear;
                cmd.Parameters.Add("dept_code", OracleDbType.Decimal).Value = deptCode;
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new GadSubheadDto
                        {
                            AcSubhead = reader["AC_SUBHEAD"] as string ?? string.Empty,
                            AcSubheadName = reader["AC_SUBHEADNAME"] as string ?? string.Empty,
                            AcSubheadNameLL = reader["AC_SUBHEADNAMELL"] as string ?? string.Empty,
                            AcSubheadNameLLUnicode = reader["AC_SUBHEADNAMELL_UNICODE"] as string ?? string.Empty,
                        });
                    }
                }
            }
            return result;
        }

        // ── Budget Info for an account subhead ───────────────────────────────────

        public GadBudgetInfoDto GetBudgetInfo(int ulbCode, int deptCode, string acSubhead, string finYear, string proposalType = null)
        {
            // Total sanctioned budget — DEPT_CODE=0 means shared across all departments
            const string budgetSql = @"
                SELECT NVL(SUM(DEBIT_AMOUNT + CREDIT_AMOUNT), 0) AS TOTAL_BUDGET
                FROM ABASDEPTBUDJETACTDET
                WHERE AC_SUBHEAD = :ac_subhead
                  AND FIN_YEAR   = :fin_year
                  AND (DEPT_CODE = :dept_code OR DEPT_CODE = 0)";

            // Committed expenditure: sum of PROPOSEDCOST from both quotation and tender proposals
            // for this budget code and financial year (approved proposals only)
            const string committedSql = @"
                SELECT NVL(SUM(amt), 0)
                FROM (
                    SELECT NVL(PROPOSEDCOST, 0) AS amt
                    FROM GAD.GADQUOTATIONORDERSDET
                    WHERE ACHEAD_CODE = :ac_subhead
                      AND FINYEAR     = :fin_year
                      AND VALIDFLAG   = 'y'
                      AND DEPT_CODE   = :dept_code
                    UNION ALL
                    SELECT NVL(PROPOSEDCOST, 0) AS amt
                    FROM GAD.GADTENDERORDERSDET
                    WHERE ACHEAD_CODE = :ac_subhead
                      AND FINYEAR     = :fin_year
                      AND VALIDFLAG   = 'y'
                      AND DEPT_CODE   = :dept_code
                )";

            // Cap settings (if any) for this budget code / fin year
            const string capSql = @"
                SELECT CAP_PERCENTAGE, CAP_AMOUNT
                FROM GAD.GADBUDGETCAPDET
                WHERE AC_SUBHEAD = :ac_subhead
                  AND FIN_YEAR   = :fin_year
                  AND ULB_CODE   = :ulb_code
                  AND VALIDFLAG  = 'y'";

            decimal totalBudget = 0m;
            decimal committedAmt = 0m;
            decimal? capPercentage = null;
            decimal? capAmount = null;

            using (var conn = _connectionFactory.CreateAbas())
            {
                conn.Open();

                using (var cmd = new OracleCommand(budgetSql, conn))
                {
                    cmd.Parameters.Add("ac_subhead", OracleDbType.Varchar2).Value = acSubhead;
                    cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = finYear;
                    cmd.Parameters.Add("dept_code", OracleDbType.Decimal).Value = deptCode;
                    var val = cmd.ExecuteScalar();
                    if (val != null && val != DBNull.Value)
                        totalBudget = Convert.ToDecimal(val);
                }

                try
                {
                    using (var cmd = new OracleCommand(committedSql, conn))
                    {
                        cmd.Parameters.Add("ac_subhead", OracleDbType.Varchar2).Value = acSubhead;
                        cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = finYear;
                        cmd.Parameters.Add("dept_code", OracleDbType.Decimal).Value = deptCode;
                        var val = cmd.ExecuteScalar();
                        if (val != null && val != DBNull.Value)
                            committedAmt = Convert.ToDecimal(val);
                    }
                }
                catch { committedAmt = 0m; }

                // Try reading cap — table may not exist yet
                try
                {
                    using (var cmd = new OracleCommand(capSql, conn))
                    {
                        cmd.Parameters.Add("ac_subhead", OracleDbType.Varchar2).Value = acSubhead;
                        cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = finYear;
                        cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                if (reader["CAP_PERCENTAGE"] != DBNull.Value)
                                    capPercentage = Convert.ToDecimal(reader["CAP_PERCENTAGE"]);
                                if (reader["CAP_AMOUNT"] != DBNull.Value)
                                    capAmount = Convert.ToDecimal(reader["CAP_AMOUNT"]);
                            }
                        }
                    }
                }
                catch { /* cap table may not exist yet */ }
            }

            // Effective budget = cap_amount if set, else cap_percentage of total, else total
            decimal effectiveBudget = totalBudget;
            if (capAmount.HasValue && capAmount.Value > 0)
                effectiveBudget = capAmount.Value;
            else if (capPercentage.HasValue && capPercentage.Value > 0)
                effectiveBudget = Math.Round(totalBudget * capPercentage.Value / 100m, 0);

            // Apply proposal-type cap rules:
            // Quotation (Q)  → cap applies only when effectiveBudget ≤ ₹1,00,000
            // Tender    (T)  → cap applies only when effectiveBudget > ₹1,00,000
            // Other          → no cap at all (use full budget)
            var pt = (proposalType ?? string.Empty).Trim().ToUpperInvariant();
            if (pt == "OTHER")
            {
                effectiveBudget = totalBudget;
            }
            else if (pt == "Q" && effectiveBudget > 100000m)
            {
                // Cap is above ₹1 lac — not applicable to quotation proposals
                effectiveBudget = totalBudget;
            }
            else if (pt == "T" && effectiveBudget <= 100000m && effectiveBudget < totalBudget)
            {
                // Cap is ₹1 lac or under — not applicable to tender proposals
                effectiveBudget = totalBudget;
            }

            return new GadBudgetInfoDto
            {
                AcSubhead = acSubhead,
                FinYear = finYear,
                TotalBudget = totalBudget,
                EffectiveBudget = effectiveBudget,
                ActualExpenditure = committedAmt,
                RemainingBudget = effectiveBudget - committedAmt,
                CapPercentage = capPercentage,
                CapAmount = capAmount,
            };
        }

        // ── Save Under-10L (Quotation) Proposal ──────────────────────────────────

        public WorkProposalSaveResult SaveUnder10LProposal(int ulbCode, Under10LProposalRequest req)
        {
            const string seqSql = "SELECT GAD.SEQGADQUOTATION.NEXTVAL FROM DUAL";

            const string insertSql = @"
                INSERT INTO GAD.GADQUOTATIONORDERSDET
                    (ULB_CODE, NASTINO, FINYEAR, PROPOSALDATE,
                     DEPT_CODE, ORDERNO,
                     PROPOSALNAME, WORKPLACE, WORKPLACEMAP, WARDNO,
                     WORKNEED, WORKDONEBEFORE,
                     PROPOSALAMOUNT, PROPOSEDCOST, AVAILABLEBUDGET,
                     TECHAPPROVEBYOFFICER, TECHSANCTIONNO, TECHSANCTIONDATE, COSTFINALIAZED,
                     PLACEOFCORPORATION, NOCATTACHED, NOCOFOWNER,
                     ANYDISPUTEONPLACE, ANYCASESONPLACE, CASEDETAILS,
                     VERIFIDEBYTOWNPLAN, APPROVALBYTOWNPLAN, EXPENSEVALID,
                     STOCKLISTATTACHED, PICATTACHED,
                     ACHEAD_CODE, ACHEAD_NAME,
                     BUDGETFORWORK, PRRPOSALFROMOTHERDEPT, WORKSPLITTED,
                     WORKEXAMINPERIOD, PROPOSEDBEFOREEXAMIN, SANCTIONOFFICERS,
                     STATUS, VALIDFLAG, ENTBY, ENTDT,
                     OTHERREMARKS, STAGENO, REF_NO, INWARD_TYPE)
                VALUES
                    (:ulb_code, :nastino, :fin_year, SYSDATE,
                     :dept_code, 0,
                     :proposalname, :workplace, :workplacemap, :wardno,
                     :workneed, :workdonebefore,
                     :proposalamount, :proposedcost, 0,
                     :techapprove, :techsanctionno, :techsanctiondate, :costfinalized,
                     :placeofcorp, :nocattached, :nocofowner,
                     :anydispute, :anycases, :casedetails,
                     :townplancheck, :townplanapproval, :expvalid,
                     :stocklist, :picattached,
                     :achead_code, '.',
                     :budgetforwork, :otherdept, :worksplitted,
                     :workexaminperiod, :proposedbeforeexamin, :sanctionofficers,
                     'n', 'y', :entby, SYSDATE,
                     :otherremarks, 1, GAD.SEQQUOTTENREFNO.NEXTVAL, :inward_type)";

            try
            {
                using (var conn = _connectionFactory.CreateAbas())
                {
                    conn.Open();
                    long orderNo = req.PreGeneratedOrderNo > 0
                        ? req.PreGeneratedOrderNo
                        : Convert.ToInt64(new OracleCommand(seqSql, conn).ExecuteScalar());

                    using (var cmd = new OracleCommand(insertSql, conn))
                    {
                        AddCommonParams(cmd, ulbCode, orderNo, req);
                        cmd.Parameters.Add("inward_type", OracleDbType.Varchar2).Value =
                            string.IsNullOrWhiteSpace(req.InwardType) ? "quotation" : req.InwardType.Trim();
                        cmd.ExecuteNonQuery();
                    }
                    return new WorkProposalSaveResult { Success = true, OrderNo = orderNo, Message = "Quotation proposal saved successfully." };
                }
            }
            catch (Exception ex)
            {
                return new WorkProposalSaveResult { Success = false, Message = ex.Message };
            }
        }

        // ── Save Over-10L (Tender) Proposal ──────────────────────────────────────

        public WorkProposalSaveResult SaveOver10LProposal(int ulbCode, Over10LProposalRequest req)
        {
            const string seqSql = "SELECT GAD.SEQGADTENDERS.NEXTVAL FROM DUAL";

            const string insertSql = @"
                INSERT INTO GAD.GADTENDERORDERSDET
                    (ULB_CODE, NASTINO, FINYEAR, PROPOSALDATE,
                     DEPT_CODE, ORDERNO,
                     PROPOSALNAME, WORKPLACE, WORKPLACEMAP, WARDNO,
                     WORKNEED, WORKDONEBEFORE,
                     PROPOSALAMOUNT, PROPOSEDCOST, AVAILABLEBUDGET,
                     TECHAPPROVEBYOFFICER, TECHSANCTIONNO, TECHSANCTIONDATE, COSTFINALIAZED,
                     PLACEOFCORPORATION, NOCATTACHED, NOCOFOWNER,
                     ANYDISPUTEONPLACE, ANYCASESONPLACE, CASEDETAILS,
                     VERIFIDEBYTOWNPLAN, APPROVALBYTOWNPLAN, EXPENSEVALID,
                     STOCKLISTATTACHED, PICATTACHED,
                     ACHEAD_CODE, ACHEAD_NAME,
                     BUDGETFORWORK, PRRPOSALFROMOTHERDEPT, WORKSPLITTED,
                     WORKEXAMINPERIOD, PROPOSEDBEFOREEXAMIN, SANCTIONOFFICERS,
                     STATUS, VALIDFLAG, ENTBY, ENTDT,
                     OTHERREMARKS, STAGENO, REF_NO)
                VALUES
                    (:ulb_code, :nastino, :fin_year, SYSDATE,
                     :dept_code, 0,
                     :proposalname, :workplace, :workplacemap, :wardno,
                     :workneed, :workdonebefore,
                     :proposalamount, :proposedcost, 0,
                     :techapprove, :techsanctionno, :techsanctiondate, :costfinalized,
                     :placeofcorp, :nocattached, :nocofowner,
                     :anydispute, :anycases, :casedetails,
                     :townplancheck, :townplanapproval, :expvalid,
                     :stocklist, :picattached,
                     :achead_code, '.',
                     :budgetforwork, :otherdept, :worksplitted,
                     :workexaminperiod, :proposedbeforeexamin, :sanctionofficers,
                     'n', 'y', :entby, SYSDATE,
                     :otherremarks, 1, GAD.SEQQUOTTENREFNO.NEXTVAL)";

            try
            {
                using (var conn = _connectionFactory.CreateAbas())
                {
                    conn.Open();
                    long orderNo = req.PreGeneratedOrderNo > 0
                        ? req.PreGeneratedOrderNo
                        : Convert.ToInt64(new OracleCommand(seqSql, conn).ExecuteScalar());

                    using (var cmd = new OracleCommand(insertSql, conn))
                    {
                        AddCommonParams(cmd, ulbCode, orderNo, req);
                        cmd.ExecuteNonQuery();
                    }
                    return new WorkProposalSaveResult { Success = true, OrderNo = orderNo, Message = "Tender proposal saved successfully." };
                }
            }
            catch (Exception ex)
            {
                return new WorkProposalSaveResult { Success = false, Message = ex.Message };
            }
        }

        // ── Shared parameter helper ───────────────────────────────────────────────

        private static void AddCommonParams(OracleCommand cmd, int ulbCode, long orderNo, WorkProposalRequestBase req)
        {
            cmd.Parameters.Add("ulb_code",             OracleDbType.Decimal).Value  = ulbCode;
            cmd.Parameters.Add("nastino",              OracleDbType.Int64).Value    = orderNo;
            cmd.Parameters.Add("fin_year",             OracleDbType.Varchar2).Value = req.FinYear;
            cmd.Parameters.Add("dept_code",            OracleDbType.Decimal).Value  = req.DeptCode;
            cmd.Parameters.Add("proposalname",         OracleDbType.NVarchar2).Value = req.WorkName ?? string.Empty;
            cmd.Parameters.Add("workplace",            OracleDbType.NVarchar2).Value = req.WorkPlace ?? string.Empty;
            cmd.Parameters.Add("workplacemap",         OracleDbType.Varchar2).Value = req.MapAttached ?? string.Empty;
            cmd.Parameters.Add("wardno",               OracleDbType.Varchar2).Value = req.WardNos ?? string.Empty;
            cmd.Parameters.Add("workneed",             OracleDbType.NVarchar2).Value = req.WorkNeed ?? string.Empty;
            cmd.Parameters.Add("workdonebefore",       OracleDbType.NVarchar2).Value = req.WorkDoneBefore ?? string.Empty;
            cmd.Parameters.Add("proposalamount",       OracleDbType.Decimal).Value  = req.WorkAmount;
            cmd.Parameters.Add("proposedcost",         OracleDbType.Decimal).Value  = req.ProposalCost;
            cmd.Parameters.Add("techapprove",          OracleDbType.Varchar2).Value = req.TechApproval ?? string.Empty;
            cmd.Parameters.Add("techsanctionno",       OracleDbType.NVarchar2).Value = req.TechSanctionNo ?? string.Empty;

            var tsdParam = cmd.Parameters.Add("techsanctiondate", OracleDbType.Date);
            if (!string.IsNullOrWhiteSpace(req.TechSanctionDate) && DateTime.TryParse(req.TechSanctionDate, out var tsd))
                tsdParam.Value = tsd;
            else
                tsdParam.Value = DBNull.Value;

            cmd.Parameters.Add("costfinalized",        OracleDbType.Varchar2).Value = req.DsrRates ?? string.Empty;
            cmd.Parameters.Add("placeofcorp",          OracleDbType.Varchar2).Value = req.PlaceOwnership ?? string.Empty;
            cmd.Parameters.Add("nocattached",          OracleDbType.Varchar2).Value = req.NocDocAttached ?? string.Empty;
            cmd.Parameters.Add("nocofowner",           OracleDbType.Varchar2).Value = req.NocCertificate ?? string.Empty;
            cmd.Parameters.Add("anydispute",           OracleDbType.Varchar2).Value = req.AnyDispute ?? string.Empty;
            cmd.Parameters.Add("anycases",             OracleDbType.Varchar2).Value = req.CourtCase ?? string.Empty;
            cmd.Parameters.Add("casedetails",          OracleDbType.NVarchar2).Value = req.CaseDetails ?? string.Empty;
            cmd.Parameters.Add("townplancheck",        OracleDbType.Varchar2).Value = req.TownPlanCheck ?? string.Empty;
            cmd.Parameters.Add("townplanapproval",     OracleDbType.Varchar2).Value = req.TownPlanApproval ?? string.Empty;
            cmd.Parameters.Add("expvalid",             OracleDbType.Varchar2).Value = req.ExpendValid ?? string.Empty;
            cmd.Parameters.Add("stocklist",            OracleDbType.Varchar2).Value = req.StockListAttached ?? string.Empty;
            cmd.Parameters.Add("picattached",          OracleDbType.Varchar2).Value = req.PhotoAttached ?? string.Empty;
            cmd.Parameters.Add("achead_code",          OracleDbType.Varchar2).Value = req.AcSubhead ?? string.Empty;
            cmd.Parameters.Add("budgetforwork",        OracleDbType.Varchar2).Value = req.AcHeadValid ?? string.Empty;
            cmd.Parameters.Add("otherdept",            OracleDbType.Varchar2).Value = req.OtherDept ?? string.Empty;
            cmd.Parameters.Add("worksplitted",         OracleDbType.Varchar2).Value = req.WorkSplit ?? string.Empty;
            cmd.Parameters.Add("workexaminperiod",     OracleDbType.NVarchar2).Value = req.MaintenancePeriod ?? string.Empty;
            cmd.Parameters.Add("proposedbeforeexamin", OracleDbType.Varchar2).Value = req.PrevMaintenance ?? string.Empty;
            cmd.Parameters.Add("sanctionofficers",     OracleDbType.NVarchar2).Value = req.CompetentOfficer ?? string.Empty;
            cmd.Parameters.Add("entby",                OracleDbType.Varchar2).Value = req.EnteredBy ?? "ERP";
            cmd.Parameters.Add("otherremarks",         OracleDbType.NVarchar2).Value = req.Remarks ?? string.Empty;
        }

        // ── GetProposals (list for dept) ─────────────────────────────────────────

        public IEnumerable<WorkProposalListDto> GetProposals(int ulbCode, string userId, string finYear, bool requireAccountRemark, string search, int pageSize, int pageNo, out int totalCount)
        {
            // PTTEST01 and ADMIN001 see all departments; all others see only their own dept
            bool showAll = string.IsNullOrWhiteSpace(userId)
                || string.Equals(userId.Trim(), "PTTEST01", StringComparison.OrdinalIgnoreCase)
                || string.Equals(userId.Trim(), "ADMIN001", StringComparison.OrdinalIgnoreCase);

            bool hasSearch = !string.IsNullOrWhiteSpace(search);
            string searchFilterQ = hasSearch
                ? @"AND (UPPER(q.PROPOSALNAME) LIKE '%' || UPPER(:search) || '%'
                     OR TO_CHAR(q.NASTINO) LIKE '%' || :search || '%'
                     OR UPPER(q.ACHEAD_CODE) LIKE '%' || UPPER(:search) || '%')"
                : string.Empty;
            string searchFilterT = hasSearch
                ? @"AND (UPPER(t.PROPOSALNAME) LIKE '%' || UPPER(:search) || '%'
                     OR TO_CHAR(t.NASTINO) LIKE '%' || :search || '%'
                     OR UPPER(t.ACHEAD_CODE) LIKE '%' || UPPER(:search) || '%')"
                : string.Empty;

            // When requireAccountRemark=true (audit dept), only show proposals accounts has processed
            string acctFilter = requireAccountRemark
                ? "AND NVL(q.AUDIT_REMARK, ' ') != ' '"
                : string.Empty;
            string acctFilterT = requireAccountRemark
                ? "AND NVL(t.AUDIT_REMARK, ' ') != ' '"
                : string.Empty;

            string innerSql = showAll ? string.Format(@"
                SELECT q.NASTINO AS ORDER_NO, 'Q' AS PROPOSAL_TYPE, q.FINYEAR,
                       q.DEPT_CODE,
                       NVL(d.DEPT_NAMELL_UNICODE, NVL(d.DEPT_NAMELL, d.DEPT_NAME)) AS DEPT_NAME,
                       TO_CHAR(q.NASTINO) AS NASTI_NO, q.PROPOSALNAME AS WORK_NAME,
                       q.ACHEAD_CODE,
                       (SELECT NVL(AC_SUBHEADNAMELL_UNICODE, NVL(AC_SUBHEADNAMELL, AC_SUBHEADNAME))
                          FROM ABAS.ABASSUBHEADDET WHERE AC_SUBHEAD = q.ACHEAD_CODE AND VALIDFLAG = 'y' AND ROWNUM = 1) AS AC_SUBHEAD_NAME,
                       q.PROPOSEDCOST, q.ENTBY AS ENT_BY, q.ENTDT AS ENT_DT, q.WARDNO AS WARD_NO
                FROM GAD.GADQUOTATIONORDERSDET q
                LEFT JOIN ULBERP.DEPARTMENTDET d
                  ON d.DEPT_CODE = q.DEPT_CODE AND d.ULB_CODE = q.ULB_CODE
                WHERE q.ULB_CODE   = :ulb_code
                  AND q.VALIDFLAG  = 'y'
                  AND (:fin_year IS NULL OR q.FINYEAR = :fin_year)
                  {0}
                  {2}
                UNION ALL
                SELECT t.NASTINO AS ORDER_NO, 'T' AS PROPOSAL_TYPE, t.FINYEAR,
                       t.DEPT_CODE,
                       NVL(d.DEPT_NAMELL_UNICODE, NVL(d.DEPT_NAMELL, d.DEPT_NAME)) AS DEPT_NAME,
                       TO_CHAR(t.NASTINO) AS NASTI_NO, t.PROPOSALNAME AS WORK_NAME,
                       t.ACHEAD_CODE,
                       (SELECT NVL(AC_SUBHEADNAMELL_UNICODE, NVL(AC_SUBHEADNAMELL, AC_SUBHEADNAME))
                          FROM ABAS.ABASSUBHEADDET WHERE AC_SUBHEAD = t.ACHEAD_CODE AND VALIDFLAG = 'y' AND ROWNUM = 1) AS AC_SUBHEAD_NAME,
                       t.PROPOSEDCOST, t.ENTBY AS ENT_BY, t.ENTDT AS ENT_DT, t.WARDNO AS WARD_NO
                FROM GAD.GADTENDERORDERSDET t
                LEFT JOIN ULBERP.DEPARTMENTDET d
                  ON d.DEPT_CODE = t.DEPT_CODE AND d.ULB_CODE = t.ULB_CODE
                WHERE t.ULB_CODE   = :ulb_code
                  AND t.VALIDFLAG  = 'y'
                  AND (:fin_year IS NULL OR t.FINYEAR = :fin_year)
                  {1}
                  {3}", acctFilter, acctFilterT, searchFilterQ, searchFilterT) : string.Format(@"
                SELECT q.NASTINO AS ORDER_NO, 'Q' AS PROPOSAL_TYPE, q.FINYEAR,
                       q.DEPT_CODE,
                       NVL(d.DEPT_NAMELL_UNICODE, NVL(d.DEPT_NAMELL, d.DEPT_NAME)) AS DEPT_NAME,
                       TO_CHAR(q.NASTINO) AS NASTI_NO, q.PROPOSALNAME AS WORK_NAME,
                       q.ACHEAD_CODE,
                       (SELECT NVL(AC_SUBHEADNAMELL_UNICODE, NVL(AC_SUBHEADNAMELL, AC_SUBHEADNAME))
                          FROM ABAS.ABASSUBHEADDET WHERE AC_SUBHEAD = q.ACHEAD_CODE AND VALIDFLAG = 'y' AND ROWNUM = 1) AS AC_SUBHEAD_NAME,
                       q.PROPOSEDCOST, q.ENTBY AS ENT_BY, q.ENTDT AS ENT_DT, q.WARDNO AS WARD_NO
                FROM GAD.GADQUOTATIONORDERSDET q
                LEFT JOIN ULBERP.DEPARTMENTDET d
                  ON d.DEPT_CODE = q.DEPT_CODE AND d.ULB_CODE = q.ULB_CODE
                JOIN ULBERP.USERDET u
                  ON u.DEPT_CODE = q.DEPT_CODE AND u.ULB_CODE = q.ULB_CODE
                 AND UPPER(u.USER_ID) = :user_id
                WHERE q.ULB_CODE   = :ulb_code
                  AND q.VALIDFLAG  = 'y'
                  AND (:fin_year IS NULL OR q.FINYEAR = :fin_year)
                  {0}
                  {2}
                UNION ALL
                SELECT t.NASTINO AS ORDER_NO, 'T' AS PROPOSAL_TYPE, t.FINYEAR,
                       t.DEPT_CODE,
                       NVL(d.DEPT_NAMELL_UNICODE, NVL(d.DEPT_NAMELL, d.DEPT_NAME)) AS DEPT_NAME,
                       TO_CHAR(t.NASTINO) AS NASTI_NO, t.PROPOSALNAME AS WORK_NAME,
                       t.ACHEAD_CODE,
                       (SELECT NVL(AC_SUBHEADNAMELL_UNICODE, NVL(AC_SUBHEADNAMELL, AC_SUBHEADNAME))
                          FROM ABAS.ABASSUBHEADDET WHERE AC_SUBHEAD = t.ACHEAD_CODE AND VALIDFLAG = 'y' AND ROWNUM = 1) AS AC_SUBHEAD_NAME,
                       t.PROPOSEDCOST, t.ENTBY AS ENT_BY, t.ENTDT AS ENT_DT, t.WARDNO AS WARD_NO
                FROM GAD.GADTENDERORDERSDET t
                LEFT JOIN ULBERP.DEPARTMENTDET d
                  ON d.DEPT_CODE = t.DEPT_CODE AND d.ULB_CODE = t.ULB_CODE
                JOIN ULBERP.USERDET u
                  ON u.DEPT_CODE = t.DEPT_CODE AND u.ULB_CODE = t.ULB_CODE
                 AND UPPER(u.USER_ID) = :user_id
                WHERE t.ULB_CODE   = :ulb_code
                  AND t.VALIDFLAG  = 'y'
                  AND (:fin_year IS NULL OR t.FINYEAR = :fin_year)
                  {1}
                  {3}", acctFilter, acctFilterT, searchFilterQ, searchFilterT);

            int offset = (pageNo - 1) * pageSize;
            string countSql = "SELECT COUNT(*) FROM (" + innerSql + ")";
            string dataSql  = "SELECT * FROM (" + innerSql + ") ORDER BY ENT_DT DESC OFFSET :offset ROWS FETCH NEXT :page_size ROWS ONLY";

            var result = new List<WorkProposalListDto>();
            using (var conn = _connectionFactory.CreateAbas())
            {
                conn.Open();

                // Count
                using (var countCmd = new OracleCommand(countSql, conn))
                {
                    countCmd.BindByName = true;
                    countCmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                    countCmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value =
                        string.IsNullOrWhiteSpace(finYear) ? (object)DBNull.Value : finYear;
                    if (!showAll)
                        countCmd.Parameters.Add("user_id", OracleDbType.Varchar2).Value = userId.Trim().ToUpper();
                    if (hasSearch)
                        countCmd.Parameters.Add("search", OracleDbType.Varchar2).Value = search.Trim();
                    totalCount = Convert.ToInt32(countCmd.ExecuteScalar() ?? 0);
                }

                // Data
                using (var cmd = new OracleCommand(dataSql, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                    cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value =
                        string.IsNullOrWhiteSpace(finYear) ? (object)DBNull.Value : finYear;
                    if (!showAll)
                        cmd.Parameters.Add("user_id", OracleDbType.Varchar2).Value = userId.Trim().ToUpper();
                    if (hasSearch)
                        cmd.Parameters.Add("search", OracleDbType.Varchar2).Value = search.Trim();
                    cmd.Parameters.Add("offset",    OracleDbType.Int32).Value = offset;
                    cmd.Parameters.Add("page_size", OracleDbType.Int32).Value = pageSize;
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            result.Add(new WorkProposalListDto
                            {
                                OrderNo      = Convert.ToInt64(r["ORDER_NO"]),
                                ProposalType = r["PROPOSAL_TYPE"] as string ?? string.Empty,
                                FinYear      = r["FINYEAR"] as string ?? string.Empty,
                                DeptCode     = Convert.ToInt32(r["DEPT_CODE"]),
                                DeptName     = r["DEPT_NAME"] as string ?? string.Empty,
                                NastiNo      = r["NASTI_NO"] as string ?? string.Empty,
                                WorkName     = r["WORK_NAME"] as string ?? string.Empty,
                                AcSubhead    = r["ACHEAD_CODE"] as string ?? string.Empty,
                                AcSubheadName = r["AC_SUBHEAD_NAME"] as string ?? string.Empty,
                                ProposalCost = r["PROPOSEDCOST"] == DBNull.Value ? 0m : Convert.ToDecimal(r["PROPOSEDCOST"]),
                                EnteredBy    = r["ENT_BY"] as string ?? string.Empty,
                                EntryDate    = r["ENT_DT"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r["ENT_DT"]),
                                WardNos      = r["WARD_NO"] as string ?? string.Empty,
                            });
                        }
                    }
                }
            }
            return result;
        }

        // ── GetProposalDetail (full detail for print) ────────────────────────────

        public WorkProposalDetailDto GetProposalDetail(int ulbCode, string proposalType, long orderNo)
        {
            bool isQuotation = string.Equals(proposalType, "Q", StringComparison.OrdinalIgnoreCase);
            string tableName = isQuotation ? "GAD.GADQUOTATIONORDERSDET" : "GAD.GADTENDERORDERSDET";

            string sql = string.Format(@"
                SELECT t.NASTINO AS ORDER_NO, '{0}' AS PROPOSAL_TYPE, t.FINYEAR, t.DEPT_CODE,
                       NVL(d.DEPT_NAMELL_UNICODE, NVL(d.DEPT_NAMELL, d.DEPT_NAME)) AS DEPT_NAME,
                       '' AS NASTI_TYPE, TO_CHAR(t.NASTINO) AS NASTI_NO,
                       t.PROPOSALNAME AS WORK_NAME, t.WORKPLACE AS WORK_PLACE,
                       t.WORKPLACEMAP AS MAP_ATTACHED, t.WARDNO AS WARD_NO,
                       t.WORKNEED AS WORK_NEED, t.WORKDONEBEFORE AS PREV_WORK,
                       t.PROPOSALAMOUNT AS WORK_AMOUNT,
                       t.TECHAPPROVEBYOFFICER AS TECH_APPROVAL,
                       t.TECHSANCTIONNO AS TECH_SANCTION_NO,
                       TO_CHAR(t.TECHSANCTIONDATE, 'YYYY-MM-DD') AS TECH_SANCTION_DT,
                       t.COSTFINALIAZED AS DSR_RATES,
                       t.PLACEOFCORPORATION AS PLACE_OWNERSHIP,
                       t.NOCATTACHED AS NOC_DOC, t.NOCOFOWNER AS NOC_CERT,
                       t.ANYDISPUTEONPLACE AS ANY_DISPUTE, t.ANYCASESONPLACE AS COURT_CASE,
                       t.CASEDETAILS AS CASE_DETAILS,
                       t.VERIFIDEBYTOWNPLAN AS TOWN_PLAN_CHECK,
                       t.APPROVALBYTOWNPLAN AS TOWN_PLAN_APPROVAL,
                       t.EXPENSEVALID AS EXP_VALID,
                       t.STOCKLISTATTACHED AS STOCK_LIST, t.PICATTACHED AS PIC_ATTACHED,
                       t.ACHEAD_CODE,
                       t.PROPOSEDCOST, t.AVAILABLEBUDGET AS BUDGET_AMT,
                       t.BUDGETFORWORK AS VALID_AC_HEAD,
                       t.PRRPOSALFROMOTHERDEPT AS OTHER_DEPT,
                       t.WORKSPLITTED AS WORK_SPLIT,
                       t.WORKEXAMINPERIOD AS MAINTENANCE_PERIOD,
                       t.PROPOSEDBEFOREEXAMIN AS PREV_MAINTENANCE,
                       t.SANCTIONOFFICERS AS OFFICER,
                       t.OTHERREMARKS AS REMARK, t.ENTBY AS ENT_BY, t.ENTDT AS ENT_DT
                FROM {1} t
                LEFT JOIN ULBERP.DEPARTMENTDET d
                  ON d.DEPT_CODE = t.DEPT_CODE AND d.ULB_CODE = t.ULB_CODE
                WHERE t.ULB_CODE  = :ulb_code
                  AND t.NASTINO   = :order_no", isQuotation ? "Q" : "T", tableName);

            using (var conn = _connectionFactory.CreateAbas())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                cmd.Parameters.Add("order_no", OracleDbType.Int64).Value = orderNo;
                conn.Open();

                WorkProposalDetailDto dto;
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    dto = new WorkProposalDetailDto
                    {
                        OrderNo          = Convert.ToInt64(r["ORDER_NO"]),
                        ProposalType     = r["PROPOSAL_TYPE"] as string ?? string.Empty,
                        FinYear          = r["FINYEAR"] as string ?? string.Empty,
                        DeptCode         = Convert.ToInt32(r["DEPT_CODE"]),
                        DeptName         = r["DEPT_NAME"] as string ?? string.Empty,
                        NastiType        = r["NASTI_TYPE"] as string ?? string.Empty,
                        NastiNo          = r["NASTI_NO"] as string ?? string.Empty,
                        WorkName         = r["WORK_NAME"] as string ?? string.Empty,
                        WorkPlace        = r["WORK_PLACE"] as string ?? string.Empty,
                        MapAttached      = r["MAP_ATTACHED"] as string ?? string.Empty,
                        WardNos          = r["WARD_NO"] as string ?? string.Empty,
                        WorkNeed         = r["WORK_NEED"] as string ?? string.Empty,
                        WorkDoneBefore   = r["PREV_WORK"] as string ?? string.Empty,
                        WorkAmount       = r["WORK_AMOUNT"] == DBNull.Value ? 0m : Convert.ToDecimal(r["WORK_AMOUNT"]),
                        TechApproval     = r["TECH_APPROVAL"] as string ?? string.Empty,
                        TechSanctionNo   = r["TECH_SANCTION_NO"] as string ?? string.Empty,
                        TechSanctionDate = r["TECH_SANCTION_DT"] as string ?? string.Empty,
                        DsrRates         = r["DSR_RATES"] as string ?? string.Empty,
                        PlaceOwnership   = r["PLACE_OWNERSHIP"] as string ?? string.Empty,
                        NocDocAttached   = r["NOC_DOC"] as string ?? string.Empty,
                        NocCertificate   = r["NOC_CERT"] as string ?? string.Empty,
                        AnyDispute       = r["ANY_DISPUTE"] as string ?? string.Empty,
                        CourtCase        = r["COURT_CASE"] as string ?? string.Empty,
                        CaseDetails      = r["CASE_DETAILS"] as string ?? string.Empty,
                        TownPlanCheck    = r["TOWN_PLAN_CHECK"] as string ?? string.Empty,
                        TownPlanApproval = r["TOWN_PLAN_APPROVAL"] as string ?? string.Empty,
                        ExpendValid      = r["EXP_VALID"] as string ?? string.Empty,
                        StockListAttached= r["STOCK_LIST"] as string ?? string.Empty,
                        PhotoAttached    = r["PIC_ATTACHED"] as string ?? string.Empty,
                        AcSubhead        = r["ACHEAD_CODE"] as string ?? string.Empty,
                        AcSubheadName    = string.Empty, // populated below via separate query
                        ProposalCost     = r["PROPOSEDCOST"] == DBNull.Value ? 0m : Convert.ToDecimal(r["PROPOSEDCOST"]),
                        BudgetAmount     = r["BUDGET_AMT"] == DBNull.Value ? 0m : Convert.ToDecimal(r["BUDGET_AMT"]),
                        AcHeadValid      = r["VALID_AC_HEAD"] as string ?? string.Empty,
                        OtherDept        = r["OTHER_DEPT"] as string ?? string.Empty,
                        WorkSplit        = r["WORK_SPLIT"] as string ?? string.Empty,
                        MaintenancePeriod= r["MAINTENANCE_PERIOD"] as string ?? string.Empty,
                        PrevMaintenance  = r["PREV_MAINTENANCE"] as string ?? string.Empty,
                        CompetentOfficer = r["OFFICER"] as string ?? string.Empty,
                        Remarks          = r["REMARK"] as string ?? string.Empty,
                        EnteredBy        = r["ENT_BY"] as string ?? string.Empty,
                        EntryDate        = r["ENT_DT"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r["ENT_DT"]),
                    };
                }

                // Fetch subhead name in a dedicated query so ODP.NET reads NVARCHAR2
                // as a plain .NET string without any implicit Oracle charset conversion.
                if (!string.IsNullOrEmpty(dto.AcSubhead))
                {
                    const string subSql =
                        "SELECT AC_SUBHEADNAMELL_UNICODE, AC_SUBHEADNAMELL, AC_SUBHEADNAME " +
                        "FROM ABAS.ABASSUBHEADDET " +
                        "WHERE AC_SUBHEAD = :ac AND VALIDFLAG = 'y' AND ROWNUM = 1";
                    using (var cmd2 = new OracleCommand(subSql, conn))
                    {
                        cmd2.Parameters.Add("ac", OracleDbType.Varchar2).Value = dto.AcSubhead;
                        using (var r2 = cmd2.ExecuteReader())
                        {
                            if (r2.Read())
                            {
                                var unicode  = r2["AC_SUBHEADNAMELL_UNICODE"] as string ?? string.Empty;
                                var ll       = r2["AC_SUBHEADNAMELL"]         as string ?? string.Empty;
                                var eng      = r2["AC_SUBHEADNAME"]           as string ?? string.Empty;
                                dto.AcSubheadName = unicode.Length > 0 ? unicode
                                                  : ll.Length > 0     ? ll
                                                  : eng;
                            }
                        }
                    }
                }

                return dto;
            }
        }

        // ── Get next sequence value ─────────────────────────────────────────────────

        public long GetNextSequence(string proposalType)
        {
            var seqName = (proposalType ?? "Q").ToUpperInvariant() == "T"
                ? "GAD.SEQGADTENDERS"
                : "GAD.SEQGADQUOTATION";
            using (var conn = _connectionFactory.CreateAbas())
            {
                conn.Open();
                using (var cmd = new OracleCommand($"SELECT {seqName}.NEXTVAL FROM DUAL", conn))
                    return Convert.ToInt64(cmd.ExecuteScalar());
            }
        }

        // ── GetProposalForRemark ──────────────────────────────────────────────────

        public ProposalRemarkDto GetProposalForRemark(int ulbCode, string proposalType, long orderNo, string remarkType)
        {
            bool isQuotation = string.Equals(proposalType, "Q", StringComparison.OrdinalIgnoreCase);
            string tableName = isQuotation ? "GAD.GADQUOTATIONORDERSDET" : "GAD.GADTENDERORDERSDET";
            // Account dept remark → AUDIT_REMARK (legacy column name, used by accounts)
            // Audit dept remark   → REALAUDIT_REMARK
            string remarkColumn = string.Equals(remarkType, "audit", StringComparison.OrdinalIgnoreCase)
                ? "REALAUDIT_REMARK" : "AUDIT_REMARK";

            string sql = string.Format(@"
                SELECT t.NASTINO, '{0}' AS PROPOSAL_TYPE, t.FINYEAR,
                       NVL(d.DEPT_NAMELL_UNICODE, NVL(d.DEPT_NAMELL, d.DEPT_NAME)) AS DEPT_NAME,
                       TO_CHAR(t.NASTINO) AS NASTI_NO,
                       t.PROPOSALNAME AS WORK_NAME,
                       t.DEPT_CODE,
                       CASE WHEN t.AUDIT_ACHEAD_CODE IS NOT NULL AND t.AUDIT_ACHEAD_CODE != ''
                            THEN t.AUDIT_ACHEAD_CODE ELSE t.ACHEAD_CODE END AS ACHEAD_CODE,
                       CASE WHEN NVL(t.AVAILABLEBUDGET, 0) > 0
                            THEN t.AVAILABLEBUDGET
                            ELSE NVL((SELECT b.BDEBIT_AMOUNT
                                      FROM ABAS.ABASDEPTBUDJETACTDET b
                                      WHERE b.AC_SUBHEAD = t.ACHEAD_CODE
                                        AND b.ULB_CODE   = t.ULB_CODE
                                        AND b.FIN_YEAR   = t.FINYEAR
                                        AND ROWNUM = 1), 0)
                       END AS BUDGET_AMT,
                       NVL(t.PROPOSEDCOST, 0) AS PROPOSAL_COST,
                       NVL(t.{2}, ' ') AS EXISTING_REMARK
                FROM {1} t
                LEFT JOIN ULBERP.DEPARTMENTDET d
                  ON d.DEPT_CODE = t.DEPT_CODE AND d.ULB_CODE = t.ULB_CODE
                WHERE t.NASTINO  = :order_no
                  AND t.ULB_CODE = :ulb_code",
                isQuotation ? "Q" : "T", tableName, remarkColumn);

            using (var conn = _connectionFactory.CreateAbas())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.Parameters.Add("order_no", OracleDbType.Int64).Value = orderNo;
                cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                conn.Open();

                ProposalRemarkDto dto;
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    dto = new ProposalRemarkDto
                    {
                        OrderNo        = Convert.ToInt64(r["NASTINO"]),
                        ProposalType   = r["PROPOSAL_TYPE"] as string ?? string.Empty,
                        FinYear        = r["FINYEAR"] as string ?? string.Empty,
                        DeptName       = r["DEPT_NAME"] as string ?? string.Empty,
                        NastiNo        = r["NASTI_NO"] as string ?? string.Empty,
                        WorkName       = r["WORK_NAME"] as string ?? string.Empty,
                        AcSubhead      = r["ACHEAD_CODE"] as string ?? string.Empty,
                        AcSubheadName  = string.Empty,
                        DeptCode       = r["DEPT_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(r["DEPT_CODE"]),
                        BudgetAmount   = r["BUDGET_AMT"] == DBNull.Value ? 0m : Convert.ToDecimal(r["BUDGET_AMT"]),
                        ProposalCost   = r["PROPOSAL_COST"] == DBNull.Value ? 0m : Convert.ToDecimal(r["PROPOSAL_COST"]),
                        ExistingRemark = r["EXISTING_REMARK"] as string ?? string.Empty,
                    };
                }

                // Fetch subhead name separately to preserve NVARCHAR2 Unicode
                if (!string.IsNullOrEmpty(dto.AcSubhead))
                {
                    const string subSql =
                        "SELECT AC_SUBHEADNAMELL_UNICODE, AC_SUBHEADNAMELL, AC_SUBHEADNAME " +
                        "FROM ABAS.ABASSUBHEADDET " +
                        "WHERE AC_SUBHEAD = :ac AND VALIDFLAG = 'y' AND ROWNUM = 1";
                    using (var cmd2 = new OracleCommand(subSql, conn))
                    {
                        cmd2.Parameters.Add("ac", OracleDbType.Varchar2).Value = dto.AcSubhead;
                        using (var r2 = cmd2.ExecuteReader())
                        {
                            if (r2.Read())
                            {
                                var unicode = r2["AC_SUBHEADNAMELL_UNICODE"] as string ?? string.Empty;
                                var ll      = r2["AC_SUBHEADNAMELL"]         as string ?? string.Empty;
                                var eng     = r2["AC_SUBHEADNAME"]           as string ?? string.Empty;
                                dto.AcSubheadName = unicode.Length > 0 ? unicode : ll.Length > 0 ? ll : eng;
                            }
                        }
                    }
                }

                return dto;
            }
        }

        // ── SaveProposalRemark ────────────────────────────────────────────────────

        public bool SaveProposalRemark(int ulbCode, SaveRemarkRequest request)
        {
            bool isQuotation = string.Equals(request.ProposalType, "Q", StringComparison.OrdinalIgnoreCase);
            string tableName = isQuotation ? "GAD.GADQUOTATIONORDERSDET" : "GAD.GADTENDERORDERSDET";
            bool isAccount = !string.Equals(request.RemarkType, "audit", StringComparison.OrdinalIgnoreCase);

            string sql;
            if (isAccount)
            {
                // Mirrors legacy UpdateWorkAuditRemarks — accounts dept fills AUDIT_* columns
                sql = string.Format(@"
                    UPDATE {0}
                    SET AUDIT_ACHEAD_CODE      = :achead,
                        AUDIT_ACHEAD_NAME      = '',
                        AUDIT_AVAILABLEBUDGET  = :budget,
                        AUDIT_PROPOSEDCOST     = :proposalcost,
                        AUDIT_REMAININGBUDGET  = :remainingbudget,
                        AUDIT_REMARK           = :remark,
                        AUTHBY                 = :authby,
                        AUTHDT                 = SYSDATE
                    WHERE NASTINO  = :order_no
                      AND ULB_CODE = :ulb_code", tableName);
            }
            else
            {
                // Mirrors legacy UpdateWorkFinalAuditRemarks — audit dept fills REALAUDIT_REMARK
                // + sets FINALORDERAUTHBY/DT/STAGENO + inserts into GADQUOTATIONTENDERUSERFLOW
                string fileType = isQuotation ? "QT" : "TND";
                string updateSql = string.Format(@"
                    UPDATE {0}
                    SET REALAUDIT_REMARK    = :remark,
                        FINALORDERAUTHBY   = :authby,
                        FINALORDERAUTHDT   = SYSDATE,
                        STAGENO            = 3
                    WHERE NASTINO  = :order_no
                      AND ULB_CODE = :ulb_code", tableName);

                // Pre-fetch the stage number for this user to use as the upsert key
                const string stageNoSql =
                    "SELECT NVL(MAX(s.STAGENO),3) FROM GAD.GADUSERSTAGEDET s " +
                    "WHERE s.USERID = :stageUserId AND s.ULB_CODE = 1";

                // Upsert flow record: update if already exists for this nasti+stage, else insert
                const string flowUpdateSql =
                    "UPDATE GAD.GADQUOTATIONTENDERUSERFLOW " +
                    "SET REMARKS=:remarks, USERID=:userid, DEPT_CODE=:dept_code, " +
                    "    AUTHBY=:authby2, AUTHDT=SYSDATE, ENTBY=:entby, ENTDT=SYSDATE, FILETYPE=:filetype " +
                    "WHERE ULB_CODE=1 AND NASTINO=:nastino AND STAGENO=:stageno";

                const string flowInsertSql =
                    "INSERT INTO GAD.GADQUOTATIONTENDERUSERFLOW " +
                    "(ULB_CODE, FLOWCODE, NASTINO, USERID, DEPT_CODE, STAGENO, REMARKS, ENTBY, ENTDT, AUTHBY, AUTHDT, STATUS, VALIDFLAG, FILETYPE) " +
                    "VALUES(1, GAD.FLOWNO.NEXTVAL, :nastino, :userid, :dept_code, " +
                    ":stageno, :remarks, :entby, SYSDATE, :authby2, SYSDATE, 'y', 'y', :filetype)";

                using (var conn = _connectionFactory.CreateAbas())
                {
                    conn.Open();
                    // Resolve stage number outside the transaction
                    int stageNo;
                    using (var cmdS = new OracleCommand(stageNoSql, conn))
                    {
                        cmdS.BindByName = true;
                        cmdS.Parameters.Add("stageUserId", OracleDbType.Varchar2).Value = request.UserId ?? "ERP";
                        var raw = cmdS.ExecuteScalar();
                        stageNo = raw == null || raw == DBNull.Value ? 3 : Convert.ToInt32(raw);
                    }

                    using (var tx = conn.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
                    {
                        try
                        {
                            using (var cmdU = new OracleCommand(updateSql, conn))
                            {
                                cmdU.BindByName = true;
                                cmdU.Transaction = tx;
                                cmdU.Parameters.Add("remark",   OracleDbType.NVarchar2).Value = request.Remark ?? string.Empty;
                                cmdU.Parameters.Add("authby",   OracleDbType.Varchar2).Value  = request.UserId ?? "ERP";
                                cmdU.Parameters.Add("order_no", OracleDbType.Int64).Value     = request.OrderNo;
                                cmdU.Parameters.Add("ulb_code", OracleDbType.Decimal).Value   = ulbCode;
                                cmdU.ExecuteNonQuery();
                            }

                            // Try to update the existing flow row first
                            int flowRowsUpdated;
                            using (var cmdFU = new OracleCommand(flowUpdateSql, conn))
                            {
                                cmdFU.BindByName = true;
                                cmdFU.Transaction = tx;
                                cmdFU.Parameters.Add("remarks",   OracleDbType.NVarchar2).Value = request.Remark ?? string.Empty;
                                cmdFU.Parameters.Add("userid",    OracleDbType.Varchar2).Value  = request.UserId ?? "ERP";
                                cmdFU.Parameters.Add("dept_code", OracleDbType.Int32).Value     = request.DeptCode;
                                cmdFU.Parameters.Add("authby2",   OracleDbType.Varchar2).Value  = request.UserId ?? "ERP";
                                cmdFU.Parameters.Add("entby",     OracleDbType.Varchar2).Value  = request.UserId ?? "ERP";
                                cmdFU.Parameters.Add("filetype",  OracleDbType.Varchar2).Value  = fileType;
                                cmdFU.Parameters.Add("nastino",   OracleDbType.Int64).Value     = request.OrderNo;
                                cmdFU.Parameters.Add("stageno",   OracleDbType.Int32).Value     = stageNo;
                                flowRowsUpdated = cmdFU.ExecuteNonQuery();
                            }

                            // Insert only if no existing row was found
                            if (flowRowsUpdated == 0)
                            {
                                using (var cmdFI = new OracleCommand(flowInsertSql, conn))
                                {
                                    cmdFI.BindByName = true;
                                    cmdFI.Transaction = tx;
                                    cmdFI.Parameters.Add("nastino",   OracleDbType.Int64).Value     = request.OrderNo;
                                    cmdFI.Parameters.Add("userid",    OracleDbType.Varchar2).Value  = request.UserId ?? "ERP";
                                    cmdFI.Parameters.Add("dept_code", OracleDbType.Int32).Value     = request.DeptCode;
                                    cmdFI.Parameters.Add("stageno",   OracleDbType.Int32).Value     = stageNo;
                                    cmdFI.Parameters.Add("remarks",   OracleDbType.NVarchar2).Value = request.Remark ?? string.Empty;
                                    cmdFI.Parameters.Add("entby",     OracleDbType.Varchar2).Value  = request.UserId ?? "ERP";
                                    cmdFI.Parameters.Add("authby2",   OracleDbType.Varchar2).Value  = request.UserId ?? "ERP";
                                    cmdFI.Parameters.Add("filetype",  OracleDbType.Varchar2).Value  = fileType;
                                    cmdFI.ExecuteNonQuery();
                                }
                            }

                            tx.Commit();
                            return true;
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }
                }
            }

            // ── Account branch execution ─────────────────────────────────────────
            decimal remainingBudget = request.BudgetAmount - request.ProposalCost;
            using (var conn = _connectionFactory.CreateAbas())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("achead",          OracleDbType.Varchar2).Value  = request.AcSubhead ?? string.Empty;
                cmd.Parameters.Add("budget",          OracleDbType.Decimal).Value   = request.BudgetAmount;
                cmd.Parameters.Add("proposalcost",    OracleDbType.Decimal).Value   = request.ProposalCost;
                cmd.Parameters.Add("remainingbudget", OracleDbType.Decimal).Value   = remainingBudget;
                cmd.Parameters.Add("remark",          OracleDbType.NVarchar2).Value = request.Remark ?? string.Empty;
                cmd.Parameters.Add("authby",          OracleDbType.Varchar2).Value  = request.UserId ?? "ERP";
                cmd.Parameters.Add("order_no",        OracleDbType.Int64).Value     = request.OrderNo;
                cmd.Parameters.Add("ulb_code",        OracleDbType.Decimal).Value   = ulbCode;
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // ── Search proposal by NastiNo ────────────────────────────────────────────

        public NastiNoSearchResult GetProposalByNastiNo(int ulbCode, long nastiNo, string finYear = null)
        {
            // Search quotation table first (includes 'other' inward_type proposals)
            string quotSql = @"
                SELECT q.NASTINO, q.FINYEAR, q.DEPT_CODE, q.PROPOSALNAME,
                       q.ACHEAD_CODE, q.PROPOSEDCOST, q.INWARD_TYPE,
                       d.DEPT_NAMELL_UNICODE, d.DEPT_NAME,
                       s.AC_SUBHEADNAMELL_UNICODE, s.AC_SUBHEADNAMELL, s.AC_SUBHEADNAME
                FROM GAD.GADQUOTATIONORDERSDET q
                LEFT JOIN ULBERP.DEPARTMENTDET d
                       ON d.DEPT_CODE = q.DEPT_CODE AND d.ULB_CODE = q.ULB_CODE
                LEFT JOIN ABASSUBHEADDET s
                       ON s.AC_SUBHEAD = q.ACHEAD_CODE AND s.ULB_CODE = q.ULB_CODE
                WHERE q.NASTINO   = :nastino
                  AND q.ULB_CODE  = :ulb_code"
                + (string.IsNullOrWhiteSpace(finYear) ? "" : " AND q.FINYEAR = :fin_year")
                + " AND ROWNUM = 1";

            string tenderSql = @"
                SELECT t.NASTINO, t.FINYEAR, t.DEPT_CODE, t.PROPOSALNAME,
                       t.ACHEAD_CODE, t.PROPOSEDCOST,
                       d.DEPT_NAMELL_UNICODE, d.DEPT_NAME,
                       s.AC_SUBHEADNAMELL_UNICODE, s.AC_SUBHEADNAMELL, s.AC_SUBHEADNAME
                FROM GAD.GADTENDERORDERSDET t
                LEFT JOIN ULBERP.DEPARTMENTDET d
                       ON d.DEPT_CODE = t.DEPT_CODE AND d.ULB_CODE = t.ULB_CODE
                LEFT JOIN ABASSUBHEADDET s
                       ON s.AC_SUBHEAD = t.ACHEAD_CODE AND s.ULB_CODE = t.ULB_CODE
                WHERE t.NASTINO   = :nastino
                  AND t.ULB_CODE  = :ulb_code"
                + (string.IsNullOrWhiteSpace(finYear) ? "" : " AND t.FINYEAR = :fin_year")
                + " AND ROWNUM = 1";

            using (var conn = _connectionFactory.CreateAbas())
            {
                conn.Open();

                // Try quotation (includes 'other' inward_type)
                using (var cmd = new OracleCommand(quotSql, conn))
                {
                    cmd.Parameters.Add("nastino",  OracleDbType.Int64).Value   = nastiNo;
                    cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                    if (!string.IsNullOrWhiteSpace(finYear))
                        cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = finYear;

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string inwardType = reader["INWARD_TYPE"] as string ?? string.Empty;
                            string pType = string.Equals(inwardType, "quotation", StringComparison.OrdinalIgnoreCase) ? "Q" : "other";
                            return MapNastiResult(reader, pType, hasInwardType: true);
                        }
                    }
                }

                // Try tender
                using (var cmd = new OracleCommand(tenderSql, conn))
                {
                    cmd.Parameters.Add("nastino",  OracleDbType.Int64).Value   = nastiNo;
                    cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                    if (!string.IsNullOrWhiteSpace(finYear))
                        cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = finYear;

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            return MapNastiResult(reader, "T", hasInwardType: false);
                    }
                }
            }

            return null; // not found
        }

        private static NastiNoSearchResult MapNastiResult(System.Data.IDataReader reader, string proposalType, bool hasInwardType)
        {
            string deptNameUnicode = reader["DEPT_NAMELL_UNICODE"] as string ?? string.Empty;
            string deptNameLL      = reader["DEPT_NAME"] as string ?? string.Empty;
            string subheadNameU    = reader["AC_SUBHEADNAMELL_UNICODE"] as string ?? string.Empty;
            string subheadNameLL   = reader["AC_SUBHEADNAMELL"] as string ?? string.Empty;
            string subheadName     = reader["AC_SUBHEADNAME"] as string ?? string.Empty;

            return new NastiNoSearchResult
            {
                NastiNo      = Convert.ToInt64(reader["NASTINO"]),
                ProposalType = proposalType,
                FinYear      = reader["FINYEAR"] as string ?? string.Empty,
                DeptCode     = reader["DEPT_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["DEPT_CODE"]),
                DeptName     = !string.IsNullOrEmpty(deptNameUnicode) ? deptNameUnicode : deptNameLL,
                WorkName     = reader["PROPOSALNAME"] as string ?? string.Empty,
                AcSubhead    = reader["ACHEAD_CODE"] as string ?? string.Empty,
                AcSubheadName = !string.IsNullOrEmpty(subheadNameU) ? subheadNameU : (!string.IsNullOrEmpty(subheadNameLL) ? subheadNameLL : subheadName),
                ProposalCost = reader["PROPOSEDCOST"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["PROPOSEDCOST"]),
            };
        }
    }
}
