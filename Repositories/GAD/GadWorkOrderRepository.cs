using System;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using SmkcApi.Models.GAD;

namespace SmkcApi.Repositories.GAD
{
    public class GadWorkOrderRepository : IGadWorkOrderRepository
    {
        private readonly IOracleConnectionFactory _connectionFactory;

        private const string SanctionAuthorityTable = "GAD.GADWORKORDERSSANCTIONDET";
        private const string SigningOfficerTable     = "GAD.GADWORKORDERSSUGNINGDET";

        public GadWorkOrderRepository(IOracleConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException("connectionFactory");
        }

        // ── Validate Nasti (V1-V4) ──────────────────────────────────────────────

        public WorkOrderNastiValidationResult ValidateNastiForWorkOrder(int ulbCode, int nastiNo, string finYear)
        {
            var result = new WorkOrderNastiValidationResult
            {
                NastiNo            = nastiNo,
                FinYear            = finYear,
                WorkOrderNotExists = true,
            };

            const string tenderSql = @"
                SELECT a.NASTINO, a.DEPT_CODE, a.PROPOSALNAME, a.PROPOSALAMOUNT, a.ACHEAD_CODE,
                       NVL(b.DEPT_NAMELL_UNICODE, NVL(b.DEPT_NAMELL, b.DEPT_NAME)) AS DEPT_NAME,
                       a.FINYEAR
                FROM GADTENDERORDERSDET a
                JOIN ULBERP.DEPARTMENTDET b ON a.DEPT_CODE = b.DEPT_CODE
                WHERE a.ULB_CODE = :ulb_code
                  AND a.NASTINO  = :nasti_no
                  AND a.VALIDFLAG = 'y'
                  AND ROWNUM = 1";

            const string quotSql = @"
                SELECT a.NASTINO, a.DEPT_CODE, a.PROPOSALNAME, a.PROPOSALAMOUNT, a.ACHEAD_CODE,
                       NVL(b.DEPT_NAMELL_UNICODE, NVL(b.DEPT_NAMELL, b.DEPT_NAME)) AS DEPT_NAME,
                       a.FINYEAR
                FROM GADQUOTATIONORDERSDET a
                JOIN ULBERP.DEPARTMENTDET b ON a.DEPT_CODE = b.DEPT_CODE
                WHERE a.ULB_CODE = :ulb_code
                  AND a.NASTINO  = :nasti_no
                  AND a.VALIDFLAG = 'y'
                  AND ROWNUM = 1";

            // V1: Proposal lookup via GAD schema connection
            using (var conn = _connectionFactory.CreateGad())
            {
                conn.Open();
                foreach (var sql in new[] { tenderSql, quotSql })
                {
                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                        cmd.Parameters.Add("nasti_no", OracleDbType.Decimal).Value = nastiNo;
                        using (var r = cmd.ExecuteReader())
                        {
                            if (!r.Read()) continue;
                            result.NastiFound     = true;
                            result.DeptCode       = Convert.ToInt32(r["DEPT_CODE"]);
                            result.DeptName       = r["DEPT_NAME"] as string ?? string.Empty;
                            result.ProposalName   = r["PROPOSALNAME"] as string ?? string.Empty;
                            result.ProposalAmount = r["PROPOSALAMOUNT"] == DBNull.Value ? 0m : Convert.ToDecimal(r["PROPOSALAMOUNT"]);
                            result.AcSubhead      = r["ACHEAD_CODE"] as string ?? string.Empty;
                            if (string.IsNullOrWhiteSpace(result.FinYear))
                                result.FinYear    = r["FINYEAR"] as string ?? finYear;
                            break;
                        }
                    }
                }
            }

            if (!result.NastiFound)
            {
                result.ValidationMessage = "Nasti number not found in proposal records.";
                return result;
            }

            // V2/V3: Budget book entry check via ABAS connection
            const string budgetSql = @"
                SELECT BOOKENTRYNO, FINALBOOKENTRYNO, BUDGETENTBY, FINALBUDGETENTBY
                FROM ABAS.ABASBUDGETBOOKENTRY
                WHERE ULB_CODE = :ulb_code
                  AND NASTINO   = :nasti_no
                  AND NVL(CANCELLED, 'n') = 'n'
                  AND NVL(STATUS, 'y')    = 'y'
                  AND ROWNUM = 1";

            // V4/V5: GADWORKORDERSDET lookup via ABAS connection (with GAD. prefix)
            const string samajSql = @"
                SELECT WORKORDERNO, WORKORDERDATE, ELIGIBLEVENDORNAME, VENDORPROPOSEDAMOUNT, FIN_YEAR, DEPT_CODE
                FROM GAD.GADWORKORDERSDET
                WHERE ULB_CODE = :ulb_code
                  AND (FINALORDERNO = :nasti_no OR REF_NO = :nasti_no)
                  AND INWARD_TYPE = 'samaj'
                  AND NVL(VALIDFLAG, 'y') = 'y'
                  AND ROWNUM = 1";

            const string woCheckSql = @"
                SELECT WORKORDERNO, FIN_YEAR, DEPT_CODE
                FROM GAD.GADWORKORDERSDET
                WHERE ULB_CODE = :ulb_code
                  AND (FINALORDERNO = :nasti_no OR REF_NO = :nasti_no)
                  AND INWARD_TYPE = 'workorder'
                  AND NVL(VALIDFLAG, 'y') = 'y'
                  AND ROWNUM = 1";

            using (var conn = _connectionFactory.CreateAbas())
            {
                conn.Open();

                // V2/V3: Budget book entry
                using (var cmd = new OracleCommand(budgetSql, conn))
                {
                    cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                    cmd.Parameters.Add("nasti_no", OracleDbType.Decimal).Value = nastiNo;
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            result.PrimaryBookEntryNo   = r["BOOKENTRYNO"] == DBNull.Value ? 0L : Convert.ToInt64(r["BOOKENTRYNO"]);
                            result.FinalBookEntryNo     = r["FINALBOOKENTRYNO"] == DBNull.Value ? 0L : Convert.ToInt64(r["FINALBOOKENTRYNO"]);
                            result.PrimaryBookEntryDone = !string.IsNullOrWhiteSpace(r["BUDGETENTBY"] as string);
                            result.FinalBookEntryDone   = result.FinalBookEntryNo > 0 && !string.IsNullOrWhiteSpace(r["FINALBUDGETENTBY"] as string);
                        }
                    }
                }

                if (!result.FinalBookEntryDone)
                {
                    result.ValidationMessage = !result.PrimaryBookEntryDone
                        ? "Primary budget book entry has not been done for this Nasti."
                        : "Final budget book entry has not been done for this Nasti.";
                    return result;
                }

                // V4: Samaj check
                using (var cmd = new OracleCommand(samajSql, conn))
                {
                    cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value  = ulbCode;
                    cmd.Parameters.Add("nasti_no", OracleDbType.Varchar2).Value = nastiNo.ToString();
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            var samajNo   = Convert.ToInt64(r["WORKORDERNO"]);
                            var samajDept = Convert.ToInt32(r["DEPT_CODE"]);
                            var samajFy   = r["FIN_YEAR"].ToString();
                            result.SamajGenerated       = true;
                            result.SamajNo              = samajNo;
                            result.SamajDisplayNo       = string.Format("{0}/{1}/{2}", samajDept, samajNo, samajFy);
                            result.SamajDate            = r["WORKORDERDATE"] is DBNull ? string.Empty
                                                          : Convert.ToDateTime(r["WORKORDERDATE"]).ToString("dd/MM/yyyy");
                            result.EligibleVendorName   = r["ELIGIBLEVENDORNAME"] as string ?? string.Empty;
                            result.VendorProposedAmount = r["VENDORPROPOSEDAMOUNT"] == DBNull.Value ? 0m
                                                          : Convert.ToDecimal(r["VENDORPROPOSEDAMOUNT"]);
                        }
                    }
                }

                if (!result.SamajGenerated)
                {
                    result.ValidationMessage = "Samaj has not yet been generated for this Nasti.";
                    return result;
                }

                // V5: Existing work order check
                using (var cmd = new OracleCommand(woCheckSql, conn))
                {
                    cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value  = ulbCode;
                    cmd.Parameters.Add("nasti_no", OracleDbType.Varchar2).Value = nastiNo.ToString();
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            var wo   = Convert.ToInt64(r["WORKORDERNO"]);
                            var dept = Convert.ToInt32(r["DEPT_CODE"]);
                            var fy   = r["FIN_YEAR"].ToString();
                            result.WorkOrderNotExists         = false;
                            result.ExistingWorkOrderNo        = wo;
                            result.ExistingWorkOrderDisplayNo = string.Format("{0}/{1}/{2}", dept, wo, fy);
                            result.ValidationMessage          = string.Format(
                                "Work order {0} already exists for this Nasti.", result.ExistingWorkOrderDisplayNo);
                        }
                    }
                }
            }

            if (result.WorkOrderNotExists)
                result.ValidationMessage = "Nasti is valid. Ready to generate work order.";

            return result;
        }

        // ── Sanction Authorities ────────────────────────────────────────────────

        public IEnumerable<SanctionOfficialDto> GetSanctionAuthorities(int ulbCode)
        {
            var sql = string.Format(@"
                SELECT SANCTIONID, NVL(SANCTIONNAMELL, SANCTIONNAME) AS SANCTIONNAMELL
                FROM {0}
                WHERE ULB_CODE = :ulb_code AND VALIDFLAG = 'y'
                ORDER BY SANCTIONID", SanctionAuthorityTable);

            var list = new List<SanctionOfficialDto>();
            using (var conn = _connectionFactory.CreateAbas())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                conn.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new SanctionOfficialDto
                        {
                            SanctionId     = Convert.ToInt32(r["SANCTIONID"]),
                            SanctionNameLL = r["SANCTIONNAMELL"] as string ?? string.Empty,
                        });
            }
            return list;
        }

        // ── Signing Officers ────────────────────────────────────────────────────

        public IEnumerable<SanctionOfficialDto> GetSigningOfficers(int ulbCode, int deptCode)
        {
            var sql = string.Format(@"
                SELECT SANCTIONID, NVL(SANCTIONNAMELL, SANCTIONNAME) AS SANCTIONNAMELL
                FROM {0}
                WHERE ULB_CODE = :ulb_code AND DEPT_CODE = :dept_code AND VALIDFLAG = 'y'
                ORDER BY SANCTIONID", SigningOfficerTable);

            var list = new List<SanctionOfficialDto>();
            using (var conn = _connectionFactory.CreateAbas())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.Parameters.Add("ulb_code",  OracleDbType.Decimal).Value = ulbCode;
                cmd.Parameters.Add("dept_code", OracleDbType.Decimal).Value = deptCode;
                conn.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new SanctionOfficialDto
                        {
                            SanctionId     = Convert.ToInt32(r["SANCTIONID"]),
                            SanctionNameLL = r["SANCTIONNAMELL"] as string ?? string.Empty,
                        });
            }
            return list;
        }

        // ── Next Work Order Number ──────────────────────────────────────────────

        public WorkOrderNextNumberDto GetNextWorkOrderNumber(int ulbCode, int deptCode, string finYear)
        {
            const string sql = @"
                SELECT NVL(MAX(WORKORDERNO), 0) + 1 AS NEXTNO
                FROM GAD.GADWORKORDERSDET
                WHERE ULB_CODE   = :ulb_code
                  AND DEPT_CODE  = :dept_code
                  AND FIN_YEAR   = :fin_year
                  AND INWARD_TYPE IN ('samaj', 'workorder')";

            long nextNo;
            using (var conn = _connectionFactory.CreateAbas())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.Parameters.Add("ulb_code",  OracleDbType.Decimal).Value  = ulbCode;
                cmd.Parameters.Add("dept_code", OracleDbType.Decimal).Value  = deptCode;
                cmd.Parameters.Add("fin_year",  OracleDbType.Varchar2).Value = finYear;
                conn.Open();
                var val = cmd.ExecuteScalar();
                nextNo = (val == null || val == DBNull.Value) ? 1L : Convert.ToInt64(val);
            }

            return new WorkOrderNextNumberDto
            {
                NextNo    = nextNo,
                FinYear   = finYear,
                DeptCode  = deptCode,
                DisplayNo = string.Format("{0}/{1}/{2}", deptCode, nextNo, finYear),
            };
        }

        // ── Generate Work Order ─────────────────────────────────────────────────

        public WorkOrderSaveResult GenerateWorkOrder(int ulbCode, GenerateWorkOrderRequest req)
        {
            const string nextNoSql = @"
                SELECT NVL(MAX(WORKORDERNO), 0) + 1 AS NEXTNO
                FROM GAD.GADWORKORDERSDET
                WHERE ULB_CODE    = :ulb_code
                  AND DEPT_CODE   = :dept_code
                  AND FIN_YEAR    = :fin_year
                  AND INWARD_TYPE IN ('samaj', 'workorder')";

            const string insertSql = @"
                INSERT INTO GAD.GADWORKORDERSDET (
                    ULB_CODE, WORKORDERNO, FIN_YEAR, WORKORDERDATE,
                    PROPOSALNAME, PROPOSALAMOUNT, DEPT_CODE,
                    ELIGIBLEVENDORNAME, VENDORPROPOSEDAMOUNT,
                    WORKPERIOD, PERIODTYPE, SUPERVISORNAME,
                    ENTBY, ENTDT, AUTHBY, AUTHDT,
                    STATUS, VALIDFLAG, INWARD_TYPE, REF_NO, SUB_DESC, LOCCODE,
                    FINALORDERNO, AUTHORITYOFFICIALS,
                    MANUALWORKORDER, MANUALFINALORDERNO,
                    SANCTIONFILE, SANCTIONDATE,
                    SIGNING_AUTH, STAMP_AMT, CONTRACTOR_CLASS,
                    EMD_FLAG, DEPOSIT_FLAG
                ) VALUES (
                    :ulb_code, :workorderno, :fin_year, SYSDATE,
                    :proposalname, :proposalamount, :dept_code,
                    :eligiblevendorname, :vendorproposedamount,
                    :workperiod, :periodtype, :supervisorname,
                    :entby, SYSDATE, :entby, SYSDATE,
                    'y', 'y', 'workorder', :ref_no, 'na', 1,
                    :finalorderno, :authorityofficials,
                    'y', :manualfinalorderno,
                    :sanctionfile, :sanctiondate,
                    :signing_auth, :stamp_amt, '',
                    '', ''
                )";

            const string contractorSql = @"
                INSERT INTO GAD.GADCONTRACTORS
                    (ULB_CODE, WORK_ORDER_NO, DEPT_CODE, ORDER_TYPE, CONTRACTOR_NAME)
                VALUES
                    (:ulb_code, :work_order_no, :dept_code, 'workorder', :contractor_name)";

            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    using (var conn = _connectionFactory.CreateAbas())
                    {
                        conn.Open();
                        using (var tx = conn.BeginTransaction())
                        {
                            long workOrderNo;
                            using (var cmd = new OracleCommand(nextNoSql, conn))
                            {
                                cmd.Transaction = tx;
                                cmd.Parameters.Add("ulb_code",  OracleDbType.Decimal).Value  = ulbCode;
                                cmd.Parameters.Add("dept_code", OracleDbType.Decimal).Value  = req.DeptCode;
                                cmd.Parameters.Add("fin_year",  OracleDbType.Varchar2).Value = req.FinYear;
                                var val = cmd.ExecuteScalar();
                                workOrderNo = (val == null || val == DBNull.Value) ? 1L : Convert.ToInt64(val);
                            }

                            using (var cmd = new OracleCommand(insertSql, conn))
                            {
                                cmd.Transaction = tx;
                                cmd.BindByName  = true; // :entby used for both ENTBY and AUTHBY
                                cmd.Parameters.Add("ulb_code",             OracleDbType.Decimal).Value   = ulbCode;
                                cmd.Parameters.Add("workorderno",          OracleDbType.Int64).Value     = workOrderNo;
                                cmd.Parameters.Add("fin_year",             OracleDbType.Varchar2).Value  = req.FinYear;
                                cmd.Parameters.Add("proposalname",         OracleDbType.NVarchar2).Value = req.ProposalName ?? string.Empty;
                                cmd.Parameters.Add("proposalamount",       OracleDbType.Decimal).Value   = req.ProposalAmount;
                                cmd.Parameters.Add("dept_code",            OracleDbType.Decimal).Value   = req.DeptCode;
                                cmd.Parameters.Add("eligiblevendorname",   OracleDbType.NVarchar2).Value = req.EligibleVendorName ?? string.Empty;
                                cmd.Parameters.Add("vendorproposedamount", OracleDbType.Decimal).Value   = req.VendorProposedAmount;
                                cmd.Parameters.Add("workperiod",           OracleDbType.Varchar2).Value  = req.WorkPeriod ?? string.Empty;
                                cmd.Parameters.Add("periodtype",           OracleDbType.NVarchar2).Value = req.PeriodType ?? "दिवस";
                                cmd.Parameters.Add("supervisorname",       OracleDbType.NVarchar2).Value = req.SupervisorName ?? string.Empty;
                                cmd.Parameters.Add("entby",                OracleDbType.Varchar2).Value  = (req.EnteredBy ?? "ERP").ToUpper();
                                cmd.Parameters.Add("finalorderno",         OracleDbType.Varchar2).Value  = req.NastiNo ?? "0";
                                cmd.Parameters.Add("ref_no",               OracleDbType.Varchar2).Value  = req.NastiNo ?? "0";
                                cmd.Parameters.Add("authorityofficials",   OracleDbType.Varchar2).Value  = req.AuthorityOfficials.ToString();
                                cmd.Parameters.Add("manualfinalorderno",   OracleDbType.Varchar2).Value  = req.ManualFinalOrderNo ?? string.Empty;
                                cmd.Parameters.Add("sanctionfile",         OracleDbType.Varchar2).Value  = req.SanctionFile ?? string.Empty;

                                var sdParam = cmd.Parameters.Add("sanctiondate", OracleDbType.Date);
                                if (!string.IsNullOrWhiteSpace(req.SanctionDate) &&
                                    DateTime.TryParse(req.SanctionDate, out var sd))
                                    sdParam.Value = sd;
                                else
                                    sdParam.Value = DBNull.Value;

                                cmd.Parameters.Add("signing_auth", OracleDbType.Varchar2).Value = req.SigningAuth.ToString();
                                cmd.Parameters.Add("stamp_amt",    OracleDbType.Decimal).Value  = req.StampAmt;
                                cmd.ExecuteNonQuery();
                            }

                            if (req.Contractors != null)
                            {
                                foreach (var name in req.Contractors)
                                {
                                    if (string.IsNullOrWhiteSpace(name)) continue;
                                    using (var cmd = new OracleCommand(contractorSql, conn))
                                    {
                                        cmd.Transaction = tx;
                                        cmd.Parameters.Add("ulb_code",        OracleDbType.Decimal).Value   = ulbCode;
                                        cmd.Parameters.Add("work_order_no",   OracleDbType.Int64).Value     = workOrderNo;
                                        cmd.Parameters.Add("dept_code",       OracleDbType.Decimal).Value   = req.DeptCode;
                                        cmd.Parameters.Add("contractor_name", OracleDbType.NVarchar2).Value = name.Trim();
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }

                            tx.Commit();
                            return new WorkOrderSaveResult
                            {
                                Success            = true,
                                WorkOrderNo        = workOrderNo,
                                FinYear            = req.FinYear,
                                DeptCode           = req.DeptCode,
                                WorkOrderDisplayNo = string.Format("{0}/{1}/{2}", req.DeptCode, workOrderNo, req.FinYear),
                                Message            = "वर्क ऑर्डर यशस्वीपणे तयार झाली.",
                            };
                        }
                    }
                }
                catch (Oracle.ManagedDataAccess.Client.OracleException oex) when (oex.Number == 1)
                {
                    // ORA-00001: unique constraint – another concurrent insert grabbed this number; retry
                }
                catch (Exception ex)
                {
                    return new WorkOrderSaveResult { Success = false, Message = ex.Message };
                }
            }

            return new WorkOrderSaveResult
            {
                Success = false,
                Message = "वर्क ऑर्डर क्रमांक निश्चित करता आला नाही. कृपया पुन्हा प्रयत्न करा.",
            };
        }

        // ── Get Work Order Detail ───────────────────────────────────────────────

        public WorkOrderDetailDto GetWorkOrderDetail(int ulbCode, long workOrderNo, int deptCode, string finYear)
        {
            const string sql = @"
                SELECT w.WORKORDERNO, w.DEPT_CODE, w.FIN_YEAR, w.FINALORDERNO,
                       w.PROPOSALNAME, w.PROPOSALAMOUNT, w.ELIGIBLEVENDORNAME, w.VENDORPROPOSEDAMOUNT,
                       w.WORKPERIOD, w.PERIODTYPE,
                       NVL(w.MANUALFINALORDERNO, '') AS MANUALFINALORDERNO,
                       w.SANCTIONDATE, w.STAMP_AMT, w.SUPERVISORNAME, w.WORKORDERDATE,
                       NVL(d.DEPT_NAMELL_UNICODE, NVL(d.DEPT_NAMELL, d.DEPT_NAME)) AS DEPT_NAME,
                       NVL(s1.SANCTIONNAMELL, s1.SANCTIONNAME) AS AUTHORITY_NAME,
                       NVL(s2.SANCTIONNAMELL, s2.SANCTIONNAME) AS SIGNING_NAME,
                       NVL(s2.DESIGNATION, '') AS SIGNING_DESIGNATION
                FROM GAD.GADWORKORDERSDET w
                JOIN ULBERP.DEPARTMENTDET d ON w.DEPT_CODE = d.DEPT_CODE
                LEFT JOIN GAD.GADWORKORDERSSANCTIONDET s1
                    ON w.AUTHORITYOFFICIALS = s1.SANCTIONID AND s1.ULB_CODE = :ulb_code
                LEFT JOIN GAD.GADWORKORDERSSUGNINGDET s2
                    ON w.SIGNING_AUTH = s2.SANCTIONID AND s2.ULB_CODE = :ulb_code
                WHERE w.ULB_CODE    = :ulb_code
                  AND w.WORKORDERNO = :work_order_no
                  AND w.DEPT_CODE   = :dept_code
                  AND w.FIN_YEAR    = :fin_year
                  AND w.INWARD_TYPE = 'workorder'
                  AND ROWNUM = 1";

            const string contractorSql = @"
                SELECT CONTRACTOR_NAME FROM GAD.GADCONTRACTORS
                WHERE ULB_CODE = :ulb_code AND WORK_ORDER_NO = :work_order_no
                  AND DEPT_CODE = :dept_code AND ORDER_TYPE = 'workorder'";

            using (var conn = _connectionFactory.CreateAbas())
            {
                conn.Open();

                WorkOrderDetailDto detail;
                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("ulb_code",     OracleDbType.Decimal).Value  = ulbCode;
                    cmd.Parameters.Add("work_order_no",OracleDbType.Decimal).Value  = workOrderNo;
                    cmd.Parameters.Add("dept_code",    OracleDbType.Decimal).Value  = deptCode;
                    cmd.Parameters.Add("fin_year",     OracleDbType.Varchar2).Value = finYear.Trim();
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read()) return null;
                        var wo = Convert.ToInt64(r["WORKORDERNO"]);
                        var dc = Convert.ToInt32(r["DEPT_CODE"]);
                        var fy = r["FIN_YEAR"].ToString();
                        detail = new WorkOrderDetailDto
                        {
                            WorkOrderNo              = wo,
                            WorkOrderDisplayNo       = string.Format("{0}/{1}/{2}", dc, wo, fy),
                            DeptCode                 = dc,
                            FinYear                  = fy,
                            NastiNo                  = r["FINALORDERNO"].ToString(),
                            ProposalName             = r["PROPOSALNAME"].ToString(),
                            ProposalAmount           = Convert.ToDecimal(r["PROPOSALAMOUNT"]),
                            EligibleVendorName       = r["ELIGIBLEVENDORNAME"].ToString(),
                            VendorProposedAmount     = Convert.ToDecimal(r["VENDORPROPOSEDAMOUNT"]),
                            WorkPeriod               = r["WORKPERIOD"].ToString(),
                            PeriodType               = r["PERIODTYPE"].ToString(),
                            ManualFinalOrderNo       = r["MANUALFINALORDERNO"].ToString(),
                            SanctionDate             = r["SANCTIONDATE"] is DBNull ? null
                                                       : Convert.ToDateTime(r["SANCTIONDATE"]).ToString("dd/MM/yyyy"),
                            StampAmt                 = r["STAMP_AMT"] is DBNull ? 0m : Convert.ToDecimal(r["STAMP_AMT"]),
                            SupervisorName           = r["SUPERVISORNAME"].ToString(),
                            WorkOrderDate            = r["WORKORDERDATE"] is DBNull ? DateTime.Today
                                                       : Convert.ToDateTime(r["WORKORDERDATE"]),
                            DeptName                 = r["DEPT_NAME"].ToString(),
                            AuthorityName            = r["AUTHORITY_NAME"].ToString(),
                            SigningOfficerName       = r["SIGNING_NAME"].ToString(),
                            SigningOfficerDesignation= r["SIGNING_DESIGNATION"].ToString(),
                            Contractors              = new List<string>(),
                        };
                    }
                }

                using (var cmd2 = new OracleCommand(contractorSql, conn))
                {
                    cmd2.Parameters.Add("ulb_code",     OracleDbType.Decimal).Value = ulbCode;
                    cmd2.Parameters.Add("work_order_no",OracleDbType.Decimal).Value = workOrderNo;
                    cmd2.Parameters.Add("dept_code",    OracleDbType.Decimal).Value = deptCode;
                    using (var r2 = cmd2.ExecuteReader())
                        while (r2.Read())
                            detail.Contractors.Add(r2["CONTRACTOR_NAME"].ToString());
                }

                return detail;
            }
        }

        // ── Get Work Order List ─────────────────────────────────────────────────

        private static readonly System.Collections.Generic.HashSet<string> WorkOrderAdminIds =
            new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ADMIN001", "PTTEST01" };

        public IEnumerable<WorkOrderListItemDto> GetWorkOrderList(int ulbCode, string finYear, string userId)
        {
            int? filterDeptCode = null;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var uid = userId.Trim().ToUpper();
                if (!WorkOrderAdminIds.Contains(uid))
                {
                    const string userSql = @"
                        SELECT DEPT_CODE FROM ULBERP.USERDET
                        WHERE USER_ID  = :user_id
                          AND ULB_CODE = :ulb_code
                          AND ROWNUM   = 1";
                    using (var conn = _connectionFactory.CreateAbas())
                    {
                        conn.Open();
                        using (var cmd = new OracleCommand(userSql, conn))
                        {
                            cmd.BindByName = true;
                            cmd.Parameters.Add("user_id",  OracleDbType.Varchar2).Value = uid;
                            cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value  = ulbCode;
                            var raw = cmd.ExecuteScalar();
                            if (raw != null && raw != DBNull.Value)
                                filterDeptCode = Convert.ToInt32(raw);
                        }
                    }
                }
            }

            var baseSql = @"
                SELECT w.WORKORDERNO, w.FIN_YEAR, w.DEPT_CODE,
                       w.WORKORDERDATE, w.PROPOSALNAME,
                       w.ELIGIBLEVENDORNAME, w.VENDORPROPOSEDAMOUNT,
                       NVL(w.FINALORDERNO, '') AS FINALORDERNO,
                       NVL(d.DEPT_NAMELL_UNICODE, NVL(d.DEPT_NAMELL, d.DEPT_NAME)) AS DEPT_NAME
                FROM GAD.GADWORKORDERSDET w
                JOIN ULBERP.DEPARTMENTDET d ON w.DEPT_CODE = d.DEPT_CODE
                WHERE w.ULB_CODE = :ulb_code
                  AND w.FIN_YEAR = :fin_year
                  AND w.INWARD_TYPE = 'workorder'
                  AND NVL(w.VALIDFLAG, 'y') = 'y'";

            if (filterDeptCode.HasValue)
                baseSql += "\n                  AND w.DEPT_CODE = :dept_code";

            baseSql += "\n                ORDER BY w.WORKORDERDATE DESC, w.WORKORDERNO DESC";

            var result = new List<WorkOrderListItemDto>();
            using (var conn = _connectionFactory.CreateAbas())
            {
                conn.Open();
                using (var cmd = new OracleCommand(baseSql, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value  = ulbCode;
                    cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = finYear.Trim();
                    if (filterDeptCode.HasValue)
                        cmd.Parameters.Add("dept_code", OracleDbType.Decimal).Value = filterDeptCode.Value;
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            var wo   = Convert.ToInt64(r["WORKORDERNO"]);
                            var fy   = r["FIN_YEAR"].ToString();
                            var dept = Convert.ToInt32(r["DEPT_CODE"]);
                            result.Add(new WorkOrderListItemDto
                            {
                                WorkOrderNo          = wo,
                                WorkOrderDisplayNo   = string.Format("{0}/{1}/{2}", dept, wo, fy),
                                DeptCode             = dept,
                                DeptName             = r["DEPT_NAME"].ToString(),
                                FinYear              = fy,
                                WorkOrderDate        = r["WORKORDERDATE"] is DBNull ? string.Empty
                                                       : Convert.ToDateTime(r["WORKORDERDATE"]).ToString("dd/MM/yyyy"),
                                ProposalName         = r["PROPOSALNAME"].ToString(),
                                EligibleVendorName   = r["ELIGIBLEVENDORNAME"].ToString(),
                                VendorProposedAmount = Convert.ToDecimal(r["VENDORPROPOSEDAMOUNT"]),
                                NastiNo              = r["FINALORDERNO"].ToString(),
                            });
                        }
                    }
                }
            }
            return result;
        }
    }
}
