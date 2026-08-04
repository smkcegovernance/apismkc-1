using System;
using System.Collections.Generic;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using SmkcApi.Models.GAD;

namespace SmkcApi.Repositories.GAD
{
    public class GadSamajRepository : IGadSamajRepository
    {
        private readonly IOracleConnectionFactory _connectionFactory;

        // ── Table name constants ──────────────────────────────────────────────────
        // Approving-authority dropdown (no dept_code column)
        private const string SanctionAuthorityTable = "GAD.GADWORKORDERSSANCTIONDET";
        // Signing-officer dropdown (has dept_code column)
        private const string SigningOfficerTable = "GAD.GADWORKORDERSSUGNINGDET";

        public GadSamajRepository(IOracleConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException("connectionFactory");
        }

        // ── Validate Order by Nasti Number ────────────────────────────────────────

        public NastiOrderDto GetOrderByNastiNo(int ulbCode, int nastiNo)
        {
            const string tenderSql = @"
                SELECT a.NASTINO, a.ORDERNO, a.DEPT_CODE, a.PROPOSALNAME, a.PROPOSALAMOUNT,
                       a.ACHEAD_CODE,
                       NVL(b.DEPT_NAMELL_UNICODE, NVL(b.DEPT_NAMELL, b.DEPT_NAME)) AS DEPT_NAME,
                       a.FINYEAR
                FROM GADTENDERORDERSDET a
                JOIN ULBERP.DEPARTMENTDET b ON a.DEPT_CODE = b.DEPT_CODE
                WHERE a.ULB_CODE = :ulb_code
                  AND a.NASTINO  = :nasti_no
                  AND a.VALIDFLAG = 'y'
                  AND ROWNUM = 1";

            const string quotSql = @"
                SELECT a.NASTINO, a.ORDERNO, a.DEPT_CODE, a.PROPOSALNAME, a.PROPOSALAMOUNT,
                       a.ACHEAD_CODE,
                       NVL(b.DEPT_NAMELL_UNICODE, NVL(b.DEPT_NAMELL, b.DEPT_NAME)) AS DEPT_NAME,
                       a.FINYEAR
                FROM GADQUOTATIONORDERSDET a
                JOIN ULBERP.DEPARTMENTDET b ON a.DEPT_CODE = b.DEPT_CODE
                WHERE a.ULB_CODE = :ulb_code
                  AND a.NASTINO  = :nasti_no
                  AND a.VALIDFLAG = 'y'
                  AND ROWNUM = 1";

            const string budgetSql = @"
                SELECT BOOKENTRYNO, FINALBOOKENTRYNO, BUDGETENTBY, FINALBUDGETENTBY
                FROM ABAS.ABASBUDGETBOOKENTRY
                WHERE ULB_CODE = :ulb_code
                  AND NASTINO   = :nasti_no
                  AND NVL(CANCELLED, 'n') = 'n'
                  AND NVL(STATUS, 'y')    = 'y'
                  AND ROWNUM = 1";

            NastiOrderDto dto = null;

            using (var conn = _connectionFactory.CreateGad())
            {
                conn.Open();

                // Try tender first
                using (var cmd = new OracleCommand(tenderSql, conn))
                {
                    cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                    cmd.Parameters.Add("nasti_no",  OracleDbType.Decimal).Value = nastiNo;
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read()) dto = MapNastiOrderDto(r);
                    }
                }

                // Fall back to quotation
                if (dto == null)
                {
                    using (var cmd = new OracleCommand(quotSql, conn))
                    {
                        cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                        cmd.Parameters.Add("nasti_no",  OracleDbType.Decimal).Value = nastiNo;
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read()) dto = MapNastiOrderDto(r);
                        }
                    }
                }
            }

            if (dto == null) return null;

            // Budget book entry check via ABAS connection
            using (var conn = _connectionFactory.CreateAbas())
            {
                conn.Open();
                using (var cmd = new OracleCommand(budgetSql, conn))
                {
                    cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                    cmd.Parameters.Add("nasti_no",  OracleDbType.Decimal).Value = nastiNo;
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            dto.PrimaryBookEntryNo   = r["BOOKENTRYNO"] == DBNull.Value ? 0L : Convert.ToInt64(r["BOOKENTRYNO"]);
                            dto.FinalBookEntryNo     = r["FINALBOOKENTRYNO"] == DBNull.Value ? 0L : Convert.ToInt64(r["FINALBOOKENTRYNO"]);
                            dto.PrimaryBookEntryDone = !string.IsNullOrWhiteSpace(r["BUDGETENTBY"] as string);
                            dto.FinalBookEntryDone   = dto.FinalBookEntryNo > 0 && !string.IsNullOrWhiteSpace(r["FINALBUDGETENTBY"] as string);
                        }
                    }
                }
            }

            return dto;
        }

        private NastiOrderDto MapNastiOrderDto(OracleDataReader r)
        {
            return new NastiOrderDto
            {
                NastiNo              = Convert.ToInt32(r["NASTINO"]),
                OrderId              = r["ORDERNO"] == DBNull.Value ? 0 : Convert.ToInt32(r["ORDERNO"]),
                DeptCode             = Convert.ToInt32(r["DEPT_CODE"]),
                DeptName             = r["DEPT_NAME"] as string ?? string.Empty,
                ProposalName         = r["PROPOSALNAME"] as string ?? string.Empty,
                ProposalAmount       = r["PROPOSALAMOUNT"] == DBNull.Value ? 0m : Convert.ToDecimal(r["PROPOSALAMOUNT"]),
                EligibleVendorName   = string.Empty,
                VendorProposedAmount = 0m,
                AcSubhead            = r["ACHEAD_CODE"] as string ?? string.Empty,
                FinYear              = r["FINYEAR"] as string ?? string.Empty,
            };
        }
        // ── Sanction Authorities ──────────────────────────────────────────────────

        public IEnumerable<SanctionOfficialDto> GetSanctionAuthorities(int ulbCode)
        {
            var sql = string.Format(@"
                SELECT SANCTIONID, NVL(SANCTIONNAMELL, SANCTIONNAME) AS SANCTIONNAMELL
                FROM {0}
                WHERE ULB_CODE = :ulb_code AND VALIDFLAG = 'y'
                ORDER BY SANCTIONID", SanctionAuthorityTable);

            var result = new List<SanctionOfficialDto>();
            using (var conn = _connectionFactory.CreateAbas())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        result.Add(new SanctionOfficialDto
                        {
                            SanctionId   = Convert.ToInt32(r["SANCTIONID"]),
                            SanctionNameLL = r["SANCTIONNAMELL"] as string ?? string.Empty,
                        });
                    }
                }
            }
            return result;
        }

        // ── Signing Officers ──────────────────────────────────────────────────────

        public IEnumerable<SanctionOfficialDto> GetSigningOfficers(int ulbCode, int deptCode)
        {
            var sql = string.Format(@"
                SELECT SANCTIONID, NVL(SANCTIONNAMELL, SANCTIONNAME) AS SANCTIONNAMELL
                FROM {0}
                WHERE ULB_CODE = :ulb_code AND DEPT_CODE = :dept_code AND VALIDFLAG = 'y'
                ORDER BY SANCTIONID", SigningOfficerTable);

            var result = new List<SanctionOfficialDto>();
            using (var conn = _connectionFactory.CreateAbas())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                cmd.Parameters.Add("dept_code", OracleDbType.Decimal).Value = deptCode;
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        result.Add(new SanctionOfficialDto
                        {
                            SanctionId   = Convert.ToInt32(r["SANCTIONID"]),
                            SanctionNameLL = r["SANCTIONNAMELL"] as string ?? string.Empty,
                        });
                    }
                }
            }
            return result;
        }

        // ── Next Samaj Number ─────────────────────────────────────────────────────

        public SamajNextNumberDto GetNextSamajNumber(int ulbCode, int deptCode, string finYear)
        {
            // Check across BOTH samaj and workorder so numbers are globally unique per dept/year
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
                cmd.Parameters.Add("ulb_code",  OracleDbType.Decimal).Value = ulbCode;
                cmd.Parameters.Add("dept_code", OracleDbType.Decimal).Value = deptCode;
                cmd.Parameters.Add("fin_year",  OracleDbType.Varchar2).Value = finYear;
                conn.Open();
                var val = cmd.ExecuteScalar();
                nextNo = (val == null || val == DBNull.Value) ? 1L : Convert.ToInt64(val);
            }

            return new SamajNextNumberDto
            {
                NextNo    = nextNo,
                FinYear   = finYear,
                DeptCode  = deptCode,
                DisplayNo = string.Format("{0}/{1}/{2}", deptCode, nextNo, finYear),
            };
        }

        // ── Generate Samaj ────────────────────────────────────────────────────────

        public SamajSaveResult GenerateSamaj(int ulbCode, GenerateSamajRequest req)
        {
            // Check across BOTH samaj and workorder so numbers are globally unique per dept/year
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
                    'y', 'y', 'samaj', :ref_no, 'na', 1,
                    :finalorderno, :authorityofficials,
                    :manualworkorder, :manualfinalorderno,
                    :sanctionfile, :sanctiondate,
                    :signing_auth, :stamp_amt, :contractor_class,
                    :emd_flag, :deposit_flag
                )";

            const string contractorSql = @"
                INSERT INTO GAD.GADCONTRACTORS
                    (ULB_CODE, WORK_ORDER_NO, DEPT_CODE, ORDER_TYPE, CONTRACTOR_NAME)
                VALUES
                    (:ulb_code, :work_order_no, :dept_code, 'workorder', :contractor_name)";

            // Retry up to 5 times on duplicate key collision (concurrent inserts)
            for (int attempt = 0; attempt < 5; attempt++)
            {
            try
            {
                using (var conn = _connectionFactory.CreateAbas())
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {

                    // Get next sequential work order number inside transaction
                    long workOrderNo;
                    using (var cmd = new OracleCommand(nextNoSql, conn))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add("ulb_code",  OracleDbType.Decimal).Value = ulbCode;
                        cmd.Parameters.Add("dept_code", OracleDbType.Decimal).Value = req.DeptCode;
                        cmd.Parameters.Add("fin_year",  OracleDbType.Varchar2).Value = req.FinYear;
                        var val = cmd.ExecuteScalar();
                        workOrderNo = (val == null || val == DBNull.Value) ? 1L : Convert.ToInt64(val);
                    }

                    // Insert Samaj record
                    using (var cmd = new OracleCommand(insertSql, conn))
                    {
                        cmd.Transaction = tx;
                        cmd.BindByName  = true;   // allows :entby used twice (ENTBY + AUTHBY)
                        cmd.Parameters.Add("ulb_code",           OracleDbType.Decimal).Value    = ulbCode;
                        cmd.Parameters.Add("workorderno",        OracleDbType.Int64).Value      = workOrderNo;
                        cmd.Parameters.Add("fin_year",           OracleDbType.Varchar2).Value   = req.FinYear;
                        cmd.Parameters.Add("proposalname",       OracleDbType.NVarchar2).Value  = req.ProposalName ?? string.Empty;
                        cmd.Parameters.Add("proposalamount",     OracleDbType.Decimal).Value    = req.ProposalAmount;
                        cmd.Parameters.Add("dept_code",          OracleDbType.Decimal).Value    = req.DeptCode;
                        cmd.Parameters.Add("eligiblevendorname", OracleDbType.NVarchar2).Value  = req.EligibleVendorName ?? string.Empty;
                        cmd.Parameters.Add("vendorproposedamount", OracleDbType.Decimal).Value  = req.VendorProposedAmount;
                        cmd.Parameters.Add("workperiod",         OracleDbType.Varchar2).Value   = req.WorkPeriod ?? string.Empty;
                        cmd.Parameters.Add("periodtype",         OracleDbType.NVarchar2).Value  = req.PeriodType ?? "दिवस";
                        cmd.Parameters.Add("supervisorname",     OracleDbType.NVarchar2).Value  = req.SupervisorName ?? string.Empty;
                        cmd.Parameters.Add("entby",              OracleDbType.Varchar2).Value   = (req.EnteredBy ?? "ERP").ToUpper();
                        // Store nasti number in FINALORDERNO and REF_NO for audit trail
                        cmd.Parameters.Add("finalorderno",       OracleDbType.Varchar2).Value   = req.NastiNo ?? "0";
                        cmd.Parameters.Add("ref_no",             OracleDbType.Varchar2).Value   = req.NastiNo ?? "0";
                        cmd.Parameters.Add("authorityofficials", OracleDbType.Varchar2).Value   = req.AuthorityOfficials.ToString();
                        cmd.Parameters.Add("manualworkorder",    OracleDbType.Varchar2).Value   = req.IsManualOrder ? "y" : "n";
                        cmd.Parameters.Add("manualfinalorderno", OracleDbType.Varchar2).Value   = req.ManualFinalOrderNo ?? string.Empty;
                        cmd.Parameters.Add("sanctionfile",       OracleDbType.Varchar2).Value   = req.SanctionFile ?? string.Empty;

                        var sdParam = cmd.Parameters.Add("sanctiondate", OracleDbType.Date);
                        if (!string.IsNullOrWhiteSpace(req.SanctionDate) && DateTime.TryParse(req.SanctionDate, out var sd))
                            sdParam.Value = sd;
                        else
                            sdParam.Value = DBNull.Value;

                        cmd.Parameters.Add("signing_auth",       OracleDbType.Varchar2).Value   = req.SigningAuth.ToString();
                        cmd.Parameters.Add("stamp_amt",          OracleDbType.Decimal).Value    = req.StampAmt;
                        cmd.Parameters.Add("contractor_class",   OracleDbType.Varchar2).Value   = req.ContractorClass ?? string.Empty;
                        cmd.Parameters.Add("emd_flag",           OracleDbType.Varchar2).Value   = req.EmdFlag ?? "n";
                        cmd.Parameters.Add("deposit_flag",       OracleDbType.Varchar2).Value   = req.DepositFlag ?? "n";
                        cmd.ExecuteNonQuery();
                    }

                    // Insert contractors (if any)
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
                    return new SamajSaveResult
                    {
                        Success          = true,
                        WorkOrderNo      = workOrderNo,
                        FinYear          = req.FinYear,
                        DeptCode         = req.DeptCode,
                        SamajDisplayNo   = string.Format("{0}/{1}/{2}", req.DeptCode, workOrderNo, req.FinYear),
                        Message          = "समज यशस्वीपणे तयार झाला.",
                    };
                    } // end using tx
                }     // end using conn
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException oex) when (oex.Number == 1)
            {
                // ORA-00001 unique constraint violated — another concurrent insert grabbed this number
                // Loop will retry with a fresh MAX on next attempt
            }
            catch (Exception ex)
            {
                return new SamajSaveResult { Success = false, Message = ex.Message };
            }
            } // end retry loop
            return new SamajSaveResult { Success = false, Message = "समज क्रमांक निश्चित करता आला नाही. कृपया पुन्हा प्रयत्न करा." };
        }

        // ── Get Samaj Detail (for print) ──────────────────────────────────────────

        public SamajDetailDto GetSamajDetail(int ulbCode, long workOrderNo, int deptCode, string finYear)
        {
            const string sql = @"
                SELECT w.WORKORDERNO, w.DEPT_CODE, w.FIN_YEAR, w.FINALORDERNO,
                       w.PROPOSALNAME, w.PROPOSALAMOUNT, w.ELIGIBLEVENDORNAME, w.VENDORPROPOSEDAMOUNT,
                       w.WORKPERIOD, w.PERIODTYPE,
                       NVL(w.MANUALFINALORDERNO, '') AS MANUALFINALORDERNO,
                       w.MANUALWORKORDER,
                       w.SANCTIONDATE, w.STAMP_AMT, w.CONTRACTOR_CLASS,
                       NVL(w.EMD_FLAG, 'n') AS EMD_FLAG, NVL(w.DEPOSIT_FLAG, 'n') AS DEPOSIT_FLAG,
                       w.SUPERVISORNAME, w.WORKORDERDATE,
                       NVL(d.DEPT_NAMELL_UNICODE, NVL(d.DEPT_NAMELL, d.DEPT_NAME)) AS DEPT_NAME,
                       NVL(s1.SANCTIONNAMELL, s1.SANCTIONNAME) AS AUTHORITY_NAME,
                       NVL(s2.SANCTIONNAMELL, s2.SANCTIONNAME) AS SIGNING_NAME,
                       NVL(s2.DESIGNATION, '') AS SIGNING_DESIGNATION
                FROM GAD.GADWORKORDERSDET w
                JOIN ULBERP.DEPARTMENTDET d ON w.DEPT_CODE = d.DEPT_CODE
                LEFT JOIN GAD.GADWORKORDERSSANCTIONDET s1 ON w.AUTHORITYOFFICIALS = s1.SANCTIONID AND s1.ULB_CODE = :ulb_code
                LEFT JOIN GAD.GADWORKORDERSSUGNINGDET s2 ON w.SIGNING_AUTH = s2.SANCTIONID AND s2.ULB_CODE = :ulb_code
                WHERE w.ULB_CODE = :ulb_code
                  AND w.WORKORDERNO = :work_order_no
                  AND w.DEPT_CODE = :dept_code
                  AND w.FIN_YEAR = :fin_year
                  AND w.INWARD_TYPE = 'samaj'
                  AND ROWNUM = 1";

            const string contractorSql = @"
                SELECT CONTRACTOR_NAME FROM GAD.GADCONTRACTORS
                WHERE ULB_CODE = :ulb_code AND WORK_ORDER_NO = :work_order_no
                  AND DEPT_CODE = :dept_code AND ORDER_TYPE = 'workorder'";

            using (var conn = _connectionFactory.CreateAbas())
            {
                conn.Open();

                SamajDetailDto detail;
                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                    cmd.Parameters.Add("work_order_no", OracleDbType.Decimal).Value = workOrderNo;
                    cmd.Parameters.Add("dept_code", OracleDbType.Decimal).Value = deptCode;
                    cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = finYear.Trim();

                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read()) return null;
                        detail = new SamajDetailDto
                        {
                            WorkOrderNo           = Convert.ToInt64(r["WORKORDERNO"]),
                            DeptCode              = Convert.ToInt32(r["DEPT_CODE"]),
                            FinYear               = r["FIN_YEAR"].ToString(),
                            FinalOrderNo          = r["FINALORDERNO"].ToString(),
                            ProposalName          = r["PROPOSALNAME"].ToString(),
                            ProposalAmount        = Convert.ToDecimal(r["PROPOSALAMOUNT"]),
                            EligibleVendorName    = r["ELIGIBLEVENDORNAME"].ToString(),
                            VendorProposedAmount  = Convert.ToDecimal(r["VENDORPROPOSEDAMOUNT"]),
                            WorkPeriod            = r["WORKPERIOD"].ToString(),
                            PeriodType            = r["PERIODTYPE"].ToString(),
                            ManualFinalOrderNo    = r["MANUALFINALORDERNO"].ToString(),
                            IsManualOrder         = r["MANUALWORKORDER"].ToString().Trim().ToLower() == "y",
                            SanctionDate          = r["SANCTIONDATE"] is DBNull ? null : Convert.ToDateTime(r["SANCTIONDATE"]).ToString("dd/MM/yyyy"),
                            StampAmt              = r["STAMP_AMT"] is DBNull ? 0 : Convert.ToDecimal(r["STAMP_AMT"]),
                            ContractorClass       = r["CONTRACTOR_CLASS"].ToString(),
                            EmdFlag               = r["EMD_FLAG"].ToString(),
                            DepositFlag           = r["DEPOSIT_FLAG"].ToString(),
                            AcSubhead             = string.Empty,
                            AcSubheadName         = string.Empty,
                            SupervisorName        = r["SUPERVISORNAME"].ToString(),
                            WorkOrderDate         = r["WORKORDERDATE"] is DBNull ? DateTime.Today : Convert.ToDateTime(r["WORKORDERDATE"]),
                            DeptName              = r["DEPT_NAME"].ToString(),
                            AuthorityName         = r["AUTHORITY_NAME"].ToString(),
                            SigningOfficerName        = r["SIGNING_NAME"].ToString(),
                            SigningOfficerDesignation = r["SIGNING_DESIGNATION"].ToString(),
                            Contractors               = new List<string>(),
                        };
                        detail.SamajDisplayNo = string.Format("{0}/{1}/{2}", detail.DeptCode, detail.WorkOrderNo, detail.FinYear);
                    }
                }

                using (var cmd2 = new OracleCommand(contractorSql, conn))
                {
                    cmd2.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                    cmd2.Parameters.Add("work_order_no", OracleDbType.Decimal).Value = workOrderNo;
                    cmd2.Parameters.Add("dept_code", OracleDbType.Decimal).Value = deptCode;
                    using (var r2 = cmd2.ExecuteReader())
                        while (r2.Read())
                            detail.Contractors.Add(r2["CONTRACTOR_NAME"].ToString());
                }

                return detail;
            }
        }

        // ── Get Samaj List ────────────────────────────────────────────────────────

        // Admin user IDs that bypass department filtering
        private static readonly HashSet<string> SamajAdminIds =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ADMIN001", "PTTEST01" };

        public IEnumerable<SamajListItemDto> GetSamajList(int ulbCode, string finYear, string userId = null)
        {
            // Resolve optional department filter
            int? filterDeptCode = null;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var uid = userId.Trim().ToUpper();
                if (!SamajAdminIds.Contains(uid))
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
                       NVL(d.DEPT_NAMELL_UNICODE, NVL(d.DEPT_NAMELL, d.DEPT_NAME)) AS DEPT_NAME
                FROM GAD.GADWORKORDERSDET w
                JOIN ULBERP.DEPARTMENTDET d ON w.DEPT_CODE = d.DEPT_CODE
                WHERE w.ULB_CODE = :ulb_code
                  AND w.FIN_YEAR = :fin_year
                  AND w.INWARD_TYPE = 'samaj'
                  AND NVL(w.VALIDFLAG, 'y') = 'y'";

            if (filterDeptCode.HasValue)
                baseSql += "\n                  AND w.DEPT_CODE = :dept_code";

            baseSql += "\n                ORDER BY w.WORKORDERDATE DESC, w.WORKORDERNO DESC";

            var result = new List<SamajListItemDto>();
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
                            var workOrderNo = Convert.ToInt64(r["WORKORDERNO"]);
                            var fy = r["FIN_YEAR"].ToString();
                            var deptCode = Convert.ToInt32(r["DEPT_CODE"]);
                            result.Add(new SamajListItemDto
                            {
                                WorkOrderNo          = workOrderNo,
                                SamajDisplayNo       = string.Format("{0}/{1}/{2}", deptCode, workOrderNo, fy),
                                DeptCode             = deptCode,
                                DeptName             = r["DEPT_NAME"].ToString(),
                                FinYear              = fy,
                                WorkOrderDate        = r["WORKORDERDATE"] is DBNull ? string.Empty
                                                       : Convert.ToDateTime(r["WORKORDERDATE"]).ToString("dd/MM/yyyy"),
                                ProposalName         = r["PROPOSALNAME"].ToString(),
                                EligibleVendorName   = r["ELIGIBLEVENDORNAME"].ToString(),
                                VendorProposedAmount = Convert.ToDecimal(r["VENDORPROPOSEDAMOUNT"]),
                            });
                        }
                    }
                }
            }
            return result;
        }
    }
}
