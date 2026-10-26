using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Oracle.ManagedDataAccess.Client;
using SmkcApi.Models.BMT;

namespace SmkcApi.Repositories.BMT
{
    public class BillMappingTrackingRepository : IBillMappingTrackingRepository
    {
        private readonly IOracleConnectionFactory _connectionFactory;

        public BillMappingTrackingRepository(IOracleConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException("connectionFactory");
        }

        public IEnumerable<BmtDepartmentDto> GetDepartments(int ulbCode)
        {
            const string sql = @"
                SELECT DEPT_CODE,
                       NVL(DEPT_NAMELL_UNICODE, NVL(DEPT_NAMELL, DEPT_NAME)) AS DEPT_NAME
                FROM ULBERP.DEPARTMENTDET
                WHERE ULB_CODE = :ulb_code
                                    AND DEPT_CODE BETWEEN 500 AND 544
                                    AND NVL(VALIDFLAG, 'n') = 'y'
                                    AND NVL(STATUS, 'n') = 'y'
                ORDER BY DEPT_CODE";

            var rows = new List<BmtDepartmentDto>();
            using (var conn = _connectionFactory.CreateUlberp())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new BmtDepartmentDto
                        {
                            DeptCode = ToInt(reader["DEPT_CODE"]),
                            DeptName = reader["DEPT_NAME"] as string ?? string.Empty,
                        });
                    }
                }
            }

            return rows;
        }

        public IEnumerable<BmtBudgetHeadDto> GetBudgetHeads(int ulbCode, string finYear)
        {
            const string sql = @"
                SELECT s.AC_SUBHEAD,
                       NVL(MAX(s.AC_SUBHEADNAMELL_UNICODE), MAX(s.AC_SUBHEADNAMELL)) AS AC_SUBHEAD_NAME
                FROM ABAS.ABASDEPTBUDJETACTDET d
                JOIN ABAS.ABASSUBHEADDET s
                  ON s.AC_SUBHEAD = d.AC_SUBHEAD
                 AND s.ULB_CODE = d.ULB_CODE
                WHERE d.ULB_CODE = :ulb_code
                  AND d.FIN_YEAR = :fin_year
                GROUP BY s.AC_SUBHEAD
                ORDER BY s.AC_SUBHEAD";

            var rows = new List<BmtBudgetHeadDto>();
            using (var conn = _connectionFactory.CreateAbas())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = finYear;
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new BmtBudgetHeadDto
                        {
                            AcSubhead = reader["AC_SUBHEAD"] as string ?? string.Empty,
                            AcSubheadName = (reader["AC_SUBHEAD_NAME"] as string ?? string.Empty).Trim(),
                        });
                    }
                }
            }
            return rows;
        }

        public IEnumerable<LegacyBillDto> GetEligibleLegacyBills(int ulbCode, string finYear, string acSubhead)
        {
            var rows = new List<LegacyBillDto>();
            using (var conn = _connectionFactory.CreateAbas())
            {
                conn.Open();

                var columns = GetTableColumns(conn, "OBREGULARBILLDET");
                var accountBillCol = Pick(columns, "ACCOUNTBILLNO", "ACCOUNT_BILL_NO", "ACCOUNTSBILLNO", "ACBILLNO");
                var partyCol = Pick(columns, "PARTYNAME", "CONTRACTORNAME", "VENDORNAME", "BENEFICIARYNAME");
                var billDetailsCol = Pick(columns, "BILLDETAILS", "BILL_DETAILS");
                var contractorFlagCol = Pick(columns, "CONTRACTORFLAG", "CONTRACTOR_FLAG");
                var tenNoCol = Pick(columns, "TENNO", "TEN_NO");
                var amountCol = Pick(columns, "NETAMOUNT", "NET_AMOUNT", "BILLAMOUNT", "BILL_AMOUNT", "TOTALAMOUNT", "TOTAL_AMOUNT", "PASSAMOUNT");
                var finYearCol = Pick(columns, "FIN_YEAR");
                var billDateCol = Pick(columns, "BILLDATE", "AUTHDT", "ENTDT");

                if (string.IsNullOrWhiteSpace(accountBillCol))
                    throw new InvalidOperationException("Legacy bill table does not expose Account Bill Number column.");

                var finYearExpr = !string.IsNullOrWhiteSpace(finYearCol)
                    ? "t." + finYearCol
                    : (!string.IsNullOrWhiteSpace(billDateCol) ? "ABAS.GETFINYEAR(t." + billDateCol + ")" : "''");

                string fallbackPartyExpr;
                if (!string.IsNullOrWhiteSpace(partyCol) && !string.IsNullOrWhiteSpace(billDetailsCol))
                {
                    fallbackPartyExpr = "NVL(NULLIF(TRIM(t." + partyCol + "), ''), NVL(t." + billDetailsCol + ", ''))";
                }
                else if (!string.IsNullOrWhiteSpace(partyCol))
                {
                    fallbackPartyExpr = "NVL(t." + partyCol + ", '')";
                }
                else if (!string.IsNullOrWhiteSpace(billDetailsCol))
                {
                    fallbackPartyExpr = "NVL(t." + billDetailsCol + ", '')";
                }
                else
                {
                    fallbackPartyExpr = "''";
                }

                var canResolveFromTender = !string.IsNullOrWhiteSpace(contractorFlagCol) && !string.IsNullOrWhiteSpace(tenNoCol);
                var partyExpr = canResolveFromTender
                    ? "CASE WHEN NVL(t." + contractorFlagCol + ", '1') = '0' AND t." + tenNoCol + " IS NOT NULL "
                      + "THEN NVL(NULLIF(TRIM(tb.TEN_NAME), ''), " + fallbackPartyExpr + ") "
                      + "ELSE " + fallbackPartyExpr + " END"
                    : fallbackPartyExpr;

                var amountExpr = !string.IsNullOrWhiteSpace(amountCol)
                    ? "NVL(t." + amountCol + ", 0)"
                    : "NVL(tx.BILL_AMOUNT, 0)";

                var tenderJoinSql = canResolveFromTender
                    ? @"LEFT JOIN TENDER.TENBIDDERDET tb
                      ON tb.ULB_CODE = t.ULB_CODE
                     AND tb.TEN_NO = t." + tenNoCol + @"
                     AND NVL(tb.VALIDFLAG, 'y') = 'y'"
                    : string.Empty;

                var sql = @"
                    SELECT t.REGBILLNO,
                           TO_CHAR(t.DEPTBILLNO) AS DEPTBILLNO,
                           TO_CHAR(t." + accountBillCol + @") AS ACCOUNTBILLNO,
                           " + partyExpr + @" AS PARTYNAME,
                           " + amountExpr + @" AS BILLAMOUNT,
                           NVL(t.BUDGETCODE, '') AS AC_SUBHEAD,
                           " + finYearExpr + @" AS FIN_YEAR,
                           CASE WHEN m.BMT_MAPPING_ID IS NULL THEN 0 ELSE 1 END AS IS_MAPPED,
                           NVL(m.MAP_TYPE, '') AS MAP_TYPE
                    FROM ABAS.OBREGULARBILLDET t
                    " + tenderJoinSql + @"
                    LEFT JOIN (
                        SELECT ULB_CODE,
                               REGBILLNO,
                               SUM(NVL(RATE, 0) * NVL(QUANTITY, 0)) AS BILL_AMOUNT
                        FROM ABAS.OBREGULARBILLTRANDET
                        GROUP BY ULB_CODE, REGBILLNO
                    ) tx
                      ON tx.ULB_CODE = t.ULB_CODE
                     AND tx.REGBILLNO = t.REGBILLNO
                    LEFT JOIN ABAS.BMT_BILL_MAPPING m
                      ON m.ULB_CODE = t.ULB_CODE
                     AND m.REGBILLNO = t.REGBILLNO
                     AND NVL(m.VALIDFLAG, 'y') = 'y'
                    WHERE t.ULB_CODE = :ulb_code
                      AND NVL(t.VALIDFLAG, 'y') = 'y'
                      AND t." + accountBillCol + @" IS NOT NULL
                                            AND (:ac_subhead IS NULL OR NVL(t.BUDGETCODE, '') = :ac_subhead)
                      AND " + finYearExpr + @" = :fin_year
                    ORDER BY t.REGBILLNO DESC";

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                    cmd.Parameters.Add("ac_subhead", OracleDbType.Varchar2).Value = string.IsNullOrWhiteSpace(acSubhead) ? (object)DBNull.Value : acSubhead;
                    cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = finYear;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rows.Add(new LegacyBillDto
                            {
                                RegBillNo = ToLong(reader["REGBILLNO"]),
                                DeptBillNo = Convert.ToString(reader["DEPTBILLNO"]) ?? string.Empty,
                                AccountBillNo = Convert.ToString(reader["ACCOUNTBILLNO"]) ?? string.Empty,
                                PartyName = Convert.ToString(reader["PARTYNAME"]) ?? string.Empty,
                                BillAmount = ToDecimal(reader["BILLAMOUNT"]),
                                AcSubhead = reader["AC_SUBHEAD"] as string ?? string.Empty,
                                FinYear = reader["FIN_YEAR"] as string ?? string.Empty,
                                IsMapped = ToInt(reader["IS_MAPPED"]) == 1,
                                MapType = reader["MAP_TYPE"] as string ?? string.Empty,
                            });
                        }
                    }
                }
            }
            return rows;
        }

        public BmtNastiValidationDto ValidateNastiForMapping(int ulbCode, long nastiNo, string finYear)
        {
            const string sql = @"
                SELECT b.BOOKENTRYNO,
                       b.FINALBOOKENTRYNO,
                       NVL(b.WORK_NAME, '') AS WORK_NAME,
                       NVL(b.DEPT_CODE, 0) AS DEPT_CODE,
                       NVL(b.AC_SUBHEAD, '') AS AC_SUBHEAD,
                       NVL(b.FIN_YEAR, '') AS FIN_YEAR,
                       CASE
                           WHEN NVL(b.FINAL_PROPOSEDWORK_AMOUNT, 0) > 0 THEN NVL(b.FINAL_PROPOSEDWORK_AMOUNT, 0)
                           ELSE NVL(b.PROPOSEDWORK_AMOUNT, 0)
                       END AS FINAL_COMMITMENT_AMOUNT
                FROM ABAS.ABASBUDGETBOOKENTRY b
                WHERE b.ULB_CODE = :ulb_code
                  AND b.NASTINO = :nasti_no
                  AND (b.CANCELLED IS NULL OR UPPER(b.CANCELLED) <> 'Y')
                  AND NVL(b.FINALBOOKENTRYNO, 0) > 0
                  AND (:fin_year IS NULL OR b.FIN_YEAR = :fin_year)
                ORDER BY NVL(b.FINALBUDGETENTRYDATE, b.BUDGETENTRYDATE) DESC, b.BOOKENTRYNO DESC";

            var result = new BmtNastiValidationDto
            {
                Success = false,
                NastiNo = nastiNo,
            };

            using (var conn = _connectionFactory.CreateAbas())
            {
                conn.Open();
                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                    cmd.Parameters.Add("nasti_no", OracleDbType.Decimal).Value = nastiNo;
                    cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = string.IsNullOrWhiteSpace(finYear)
                        ? (object)DBNull.Value
                        : finYear;

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            result.Message = "Final budget booking not found for this Nasti.";
                            return result;
                        }

                        result.BookEntryNo = ToLong(reader["BOOKENTRYNO"]);
                        result.FinalBookEntryNo = ToLong(reader["FINALBOOKENTRYNO"]);
                        result.WorkName = reader["WORK_NAME"] as string ?? string.Empty;
                        result.DeptCode = ToInt(reader["DEPT_CODE"]);
                        result.AcSubhead = reader["AC_SUBHEAD"] as string ?? string.Empty;
                        result.FinYear = reader["FIN_YEAR"] as string ?? string.Empty;
                    }
                }

                const string mappedSql = @"
                    SELECT NVL(SUM(MAPPED_AMOUNT), 0)
                    FROM ABAS.BMT_BILL_MAPPING
                    WHERE ULB_CODE = :ulb_code
                      AND NASTINO = :nasti_no
                      AND MAP_TYPE = 'NASTI'
                      AND NVL(VALIDFLAG, 'y') = 'y'";

                using (var cmd = new OracleCommand(mappedSql, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                    cmd.Parameters.Add("nasti_no", OracleDbType.Decimal).Value = nastiNo;
                    result.AlreadyMappedAmount = ToDecimal(cmd.ExecuteScalar());
                }
            }

            result.AvailableAmount = result.FinalCommitmentAmount - result.AlreadyMappedAmount;
            result.Success = true;
            result.Message = "Nasti is eligible for mapping.";
            return result;
        }

        public BmtOperationResult MapLegacyBillToNasti(int ulbCode, MapLegacyBillRequest request)
        {
            var validation = ValidateNastiForMapping(ulbCode, request.NastiNo, request.FinYear);
            if (!validation.Success)
                return new BmtOperationResult { Success = false, Message = validation.Message };

            if (!string.Equals(validation.AcSubhead, request.AcSubhead, StringComparison.OrdinalIgnoreCase))
            {
                return new BmtOperationResult
                {
                    Success = false,
                    Message = "Budget head mismatch between selected bill and finalized Nasti booking.",
                };
            }

            if (request.BillAmount <= 0)
                return new BmtOperationResult { Success = false, Message = "Bill amount must be greater than zero." };

            if (request.BillAmount > validation.AvailableAmount)
            {
                return new BmtOperationResult
                {
                    Success = false,
                    Message = string.Format("Mapped amount exceeds available final commitment. Available: {0:N2}", validation.AvailableAmount),
                };
            }

            using (var conn = _connectionFactory.CreateAbas())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        if (HasActiveMapping(conn, tx, ulbCode, request.RegBillNo, request.AccountBillNo))
                        {
                            tx.Rollback();
                            return new BmtOperationResult
                            {
                                Success = false,
                                Message = "This bill is already mapped.",
                            };
                        }

                        const string insertSql = @"
                            INSERT INTO ABAS.BMT_BILL_MAPPING
                                (BMT_MAPPING_ID, ULB_CODE, FIN_YEAR, AC_SUBHEAD,
                                 REGBILLNO, DEPTBILLNO, ACCOUNTBILLNO, PARTY_NAME,
                                 BILL_AMOUNT, MAP_TYPE, NASTINO, BOOKENTRYNO, FINALBOOKENTRYNO,
                                 FINAL_COMMITMENT_AMOUNT, MAPPED_AMOUNT, MAP_REASON,
                                 ENTBY, ENTDT, VALIDFLAG)
                            VALUES
                                (ABAS.SEQ_BMT_BILL_MAPPING.NEXTVAL, :ulb_code, :fin_year, :ac_subhead,
                                 :regbillno, :deptbillno, :accountbillno, :party_name,
                                 :bill_amount, 'NASTI', :nastino, :bookentryno, :finalbookentryno,
                                 :final_commitment_amount, :mapped_amount, 'Mapped to finalized nasti',
                                 :entby, SYSDATE, 'y')";

                        using (var cmd = new OracleCommand(insertSql, conn))
                        {
                            cmd.Transaction = tx;
                            cmd.BindByName = true;
                            cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                            cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = request.FinYear ?? string.Empty;
                            cmd.Parameters.Add("ac_subhead", OracleDbType.Varchar2).Value = request.AcSubhead ?? string.Empty;
                            cmd.Parameters.Add("regbillno", OracleDbType.Decimal).Value = request.RegBillNo;
                            cmd.Parameters.Add("deptbillno", OracleDbType.Varchar2).Value = request.DeptBillNo ?? string.Empty;
                            cmd.Parameters.Add("accountbillno", OracleDbType.Varchar2).Value = request.AccountBillNo ?? string.Empty;
                            cmd.Parameters.Add("party_name", OracleDbType.NVarchar2).Value = request.PartyName ?? string.Empty;
                            cmd.Parameters.Add("bill_amount", OracleDbType.Decimal).Value = request.BillAmount;
                            cmd.Parameters.Add("nastino", OracleDbType.Decimal).Value = request.NastiNo;
                            cmd.Parameters.Add("bookentryno", OracleDbType.Decimal).Value = validation.BookEntryNo;
                            cmd.Parameters.Add("finalbookentryno", OracleDbType.Decimal).Value = validation.FinalBookEntryNo;
                            cmd.Parameters.Add("final_commitment_amount", OracleDbType.Decimal).Value = validation.FinalCommitmentAmount;
                            cmd.Parameters.Add("mapped_amount", OracleDbType.Decimal).Value = request.BillAmount;
                            cmd.Parameters.Add("entby", OracleDbType.Varchar2).Value = Trunc8(request.MappedBy);
                            cmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                        return new BmtOperationResult { Success = true, Message = "Bill mapped successfully." };
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        return new BmtOperationResult { Success = false, Message = ex.Message };
                    }
                }
            }
        }

        public BmtOperationResult MarkLegacyBillAsDirect(int ulbCode, MarkDirectBillRequest request)
        {
            if (request.BillAmount <= 0)
                return new BmtOperationResult { Success = false, Message = "Bill amount must be greater than zero." };

            using (var conn = _connectionFactory.CreateAbas())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        if (HasActiveMapping(conn, tx, ulbCode, request.RegBillNo, request.AccountBillNo))
                        {
                            tx.Rollback();
                            return new BmtOperationResult { Success = false, Message = "This bill is already mapped." };
                        }

                        const string sql = @"
                            INSERT INTO ABAS.BMT_BILL_MAPPING
                                (BMT_MAPPING_ID, ULB_CODE, FIN_YEAR, AC_SUBHEAD,
                                 REGBILLNO, DEPTBILLNO, ACCOUNTBILLNO, PARTY_NAME,
                                 BILL_AMOUNT, MAP_TYPE, MAPPED_AMOUNT, MAP_REASON,
                                 ENTBY, ENTDT, VALIDFLAG)
                            VALUES
                                (ABAS.SEQ_BMT_BILL_MAPPING.NEXTVAL, :ulb_code, :fin_year, :ac_subhead,
                                 :regbillno, :deptbillno, :accountbillno, :party_name,
                                 :bill_amount, 'DIRECT', :mapped_amount, :map_reason,
                                 :entby, SYSDATE, 'y')";

                        using (var cmd = new OracleCommand(sql, conn))
                        {
                            cmd.Transaction = tx;
                            cmd.BindByName = true;
                            cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                            cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = request.FinYear ?? string.Empty;
                            cmd.Parameters.Add("ac_subhead", OracleDbType.Varchar2).Value = request.AcSubhead ?? string.Empty;
                            cmd.Parameters.Add("regbillno", OracleDbType.Decimal).Value = request.RegBillNo;
                            cmd.Parameters.Add("deptbillno", OracleDbType.Varchar2).Value = request.DeptBillNo ?? string.Empty;
                            cmd.Parameters.Add("accountbillno", OracleDbType.Varchar2).Value = request.AccountBillNo ?? string.Empty;
                            cmd.Parameters.Add("party_name", OracleDbType.NVarchar2).Value = request.PartyName ?? string.Empty;
                            cmd.Parameters.Add("bill_amount", OracleDbType.Decimal).Value = request.BillAmount;
                            cmd.Parameters.Add("mapped_amount", OracleDbType.Decimal).Value = request.BillAmount;
                            cmd.Parameters.Add("map_reason", OracleDbType.NVarchar2).Value = string.IsNullOrWhiteSpace(request.Reason)
                                ? "Direct bill (no nasti/file origin)"
                                : request.Reason.Trim();
                            cmd.Parameters.Add("entby", OracleDbType.Varchar2).Value = Trunc8(request.MappedBy);
                            cmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                        return new BmtOperationResult { Success = true, Message = "Bill marked as direct successfully." };
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        return new BmtOperationResult { Success = false, Message = ex.Message };
                    }
                }
            }
        }

        public IEnumerable<BmtMappingRowDto> ListMappings(int ulbCode, string finYear, string acSubhead, string mapType)
        {
            const string sql = @"
                SELECT BMT_MAPPING_ID, MAP_TYPE, REGBILLNO, DEPTBILLNO, ACCOUNTBILLNO,
                       PARTY_NAME, MAPPED_AMOUNT, AC_SUBHEAD, FIN_YEAR,
                       NASTINO, FINALBOOKENTRYNO, FINAL_COMMITMENT_AMOUNT,
                       MAP_REASON, ENTBY, ENTDT
                FROM ABAS.BMT_BILL_MAPPING
                WHERE ULB_CODE = :ulb_code
                  AND (:fin_year IS NULL OR FIN_YEAR = :fin_year)
                  AND (:ac_subhead IS NULL OR AC_SUBHEAD = :ac_subhead)
                  AND (:map_type IS NULL OR MAP_TYPE = :map_type)
                  AND NVL(VALIDFLAG, 'y') = 'y'
                ORDER BY ENTDT DESC, BMT_MAPPING_ID DESC";

            var rows = new List<BmtMappingRowDto>();
            using (var conn = _connectionFactory.CreateAbas())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = string.IsNullOrWhiteSpace(finYear) ? (object)DBNull.Value : finYear;
                cmd.Parameters.Add("ac_subhead", OracleDbType.Varchar2).Value = string.IsNullOrWhiteSpace(acSubhead) ? (object)DBNull.Value : acSubhead;
                cmd.Parameters.Add("map_type", OracleDbType.Varchar2).Value = string.IsNullOrWhiteSpace(mapType) ? (object)DBNull.Value : mapType;
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new BmtMappingRowDto
                        {
                            MappingId = ToLong(reader["BMT_MAPPING_ID"]),
                            MapType = reader["MAP_TYPE"] as string ?? string.Empty,
                            RegBillNo = ToLong(reader["REGBILLNO"]),
                            DeptBillNo = reader["DEPTBILLNO"] as string ?? string.Empty,
                            AccountBillNo = reader["ACCOUNTBILLNO"] as string ?? string.Empty,
                            PartyName = reader["PARTY_NAME"] as string ?? string.Empty,
                            MappedAmount = ToDecimal(reader["MAPPED_AMOUNT"]),
                            AcSubhead = reader["AC_SUBHEAD"] as string ?? string.Empty,
                            FinYear = reader["FIN_YEAR"] as string ?? string.Empty,
                            NastiNo = reader["NASTINO"] == DBNull.Value ? (long?)null : ToLong(reader["NASTINO"]),
                            FinalBookEntryNo = reader["FINALBOOKENTRYNO"] == DBNull.Value ? (long?)null : ToLong(reader["FINALBOOKENTRYNO"]),
                            FinalCommitmentAmount = reader["FINAL_COMMITMENT_AMOUNT"] == DBNull.Value ? (decimal?)null : ToDecimal(reader["FINAL_COMMITMENT_AMOUNT"]),
                            MapReason = reader["MAP_REASON"] as string ?? string.Empty,
                            EnteredBy = reader["ENTBY"] as string ?? string.Empty,
                            EnteredDate = reader["ENTDT"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["ENTDT"]),
                        });
                    }
                }
            }
            return rows;
        }

        public BmtOperationResult CreateDirectBillEntry(int ulbCode, DirectBillEntryRequest request)
        {
            var billType = (request.BillType ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(billType))
                return new BmtOperationResult { Success = false, Message = "Bill type is required." };

            if (string.Equals(billType, "OTHER", StringComparison.OrdinalIgnoreCase))
                return new BmtOperationResult { Success = false, Message = "For OTHER, enter a custom bill type manually." };

            if (request.GrossAmount <= 0)
                return new BmtOperationResult { Success = false, Message = "Gross amount must be greater than zero." };

            if (request.DeductionAmount < 0)
                return new BmtOperationResult { Success = false, Message = "Deduction amount cannot be negative." };

            if (request.NetAmount != (request.GrossAmount - request.DeductionAmount))
                return new BmtOperationResult { Success = false, Message = "Net amount must equal Gross minus Deduction." };

            using (var conn = _connectionFactory.CreateAbas())
            {
                try
                {
                    conn.Open();
                    var columns = GetTableColumns(conn, "BMT_DIRECT_BILL_ENTRY");
                    var hasBillDescription = columns.Contains("BILL_DESCRIPTION");

                    var sql = hasBillDescription
                        ? @"
                INSERT INTO ABAS.BMT_DIRECT_BILL_ENTRY
                    (DIRECT_BILL_ID, ULB_CODE, FIN_YEAR, DEPT_CODE, DEPT_NAME,
                     AC_SUBHEAD, BILL_TYPE, ACCOUNTBILLNO, BILLDATE,
                     PARTY_NAME, BILL_DESCRIPTION, GROSS_AMOUNT, DEDUCTION_AMOUNT, NET_AMOUNT,
                     REMARKS, ENTBY, ENTDT, VALIDFLAG)
                VALUES
                    (ABAS.SEQ_BMT_DIRECT_BILL_ENTRY.NEXTVAL, :ulb_code, :fin_year, :dept_code, :dept_name,
                     :ac_subhead, :bill_type, :accountbillno, :billdate,
                     :party_name, :bill_description, :gross_amount, :deduction_amount, :net_amount,
                     :remarks, :entby, SYSDATE, 'y')"
                        : @"
                INSERT INTO ABAS.BMT_DIRECT_BILL_ENTRY
                    (DIRECT_BILL_ID, ULB_CODE, FIN_YEAR, DEPT_CODE, DEPT_NAME,
                     AC_SUBHEAD, BILL_TYPE, ACCOUNTBILLNO, BILLDATE,
                     PARTY_NAME, GROSS_AMOUNT, DEDUCTION_AMOUNT, NET_AMOUNT,
                     REMARKS, ENTBY, ENTDT, VALIDFLAG)
                VALUES
                    (ABAS.SEQ_BMT_DIRECT_BILL_ENTRY.NEXTVAL, :ulb_code, :fin_year, :dept_code, :dept_name,
                     :ac_subhead, :bill_type, :accountbillno, :billdate,
                     :party_name, :gross_amount, :deduction_amount, :net_amount,
                     :remarks, :entby, SYSDATE, 'y')";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                    cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = request.FinYear ?? string.Empty;
                    cmd.Parameters.Add("dept_code", OracleDbType.Decimal).Value = request.DeptCode;
                    cmd.Parameters.Add("dept_name", OracleDbType.NVarchar2).Value = request.DeptName ?? string.Empty;
                    cmd.Parameters.Add("ac_subhead", OracleDbType.Varchar2).Value = request.AcSubhead ?? string.Empty;
                    cmd.Parameters.Add("bill_type", OracleDbType.Varchar2).Value = billType;
                    cmd.Parameters.Add("accountbillno", OracleDbType.Varchar2).Value = request.AccountBillNo ?? string.Empty;
                    cmd.Parameters.Add("billdate", OracleDbType.Date).Value = request.BillDate;
                    cmd.Parameters.Add("party_name", OracleDbType.NVarchar2).Value = request.PartyName ?? string.Empty;
                    if (hasBillDescription)
                        cmd.Parameters.Add("bill_description", OracleDbType.NVarchar2).Value = request.BillDescription ?? string.Empty;
                    cmd.Parameters.Add("gross_amount", OracleDbType.Decimal).Value = request.GrossAmount;
                    cmd.Parameters.Add("deduction_amount", OracleDbType.Decimal).Value = request.DeductionAmount;
                    cmd.Parameters.Add("net_amount", OracleDbType.Decimal).Value = request.NetAmount;
                    cmd.Parameters.Add("remarks", OracleDbType.NVarchar2).Value = request.Remarks ?? string.Empty;
                    cmd.Parameters.Add("entby", OracleDbType.Varchar2).Value = Trunc8(request.EnteredBy);
                    cmd.ExecuteNonQuery();
                    }
                    return new BmtOperationResult { Success = true, Message = "Direct bill saved successfully." };
                }
                catch (Exception ex)
                {
                    return new BmtOperationResult { Success = false, Message = ex.Message };
                }
            }
        }

        public IEnumerable<DirectBillEntryDto> ListDirectBillEntries(int ulbCode, string finYear, string acSubhead)
        {
            var rows = new List<DirectBillEntryDto>();
            using (var conn = _connectionFactory.CreateAbas())
            {
                conn.Open();
                var columns = GetTableColumns(conn, "BMT_DIRECT_BILL_ENTRY");
                var hasBillDescription = columns.Contains("BILL_DESCRIPTION");
                var billDescriptionExpr = hasBillDescription
                    ? "NVL(BILL_DESCRIPTION, '') AS BILL_DESCRIPTION"
                    : "NVL(REMARKS, '') AS BILL_DESCRIPTION";

                var sql = @"
                SELECT DIRECT_BILL_ID, FIN_YEAR, DEPT_CODE, DEPT_NAME, AC_SUBHEAD,
                       BILL_TYPE, ACCOUNTBILLNO, BILLDATE, PARTY_NAME,
                       " + billDescriptionExpr + @",
                       GROSS_AMOUNT, DEDUCTION_AMOUNT, NET_AMOUNT,
                       REMARKS, ENTBY, ENTDT
                FROM ABAS.BMT_DIRECT_BILL_ENTRY
                WHERE ULB_CODE = :ulb_code
                  AND (:fin_year IS NULL OR FIN_YEAR = :fin_year)
                  AND (:ac_subhead IS NULL OR AC_SUBHEAD = :ac_subhead)
                  AND NVL(VALIDFLAG, 'y') = 'y'
                ORDER BY ENTDT DESC, DIRECT_BILL_ID DESC";

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                    cmd.Parameters.Add("fin_year", OracleDbType.Varchar2).Value = string.IsNullOrWhiteSpace(finYear) ? (object)DBNull.Value : finYear;
                    cmd.Parameters.Add("ac_subhead", OracleDbType.Varchar2).Value = string.IsNullOrWhiteSpace(acSubhead) ? (object)DBNull.Value : acSubhead;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rows.Add(new DirectBillEntryDto
                            {
                                DirectBillId = ToLong(reader["DIRECT_BILL_ID"]),
                                FinYear = reader["FIN_YEAR"] as string ?? string.Empty,
                                DeptCode = ToInt(reader["DEPT_CODE"]),
                                DeptName = reader["DEPT_NAME"] as string ?? string.Empty,
                                AcSubhead = reader["AC_SUBHEAD"] as string ?? string.Empty,
                                BillType = reader["BILL_TYPE"] as string ?? string.Empty,
                                AccountBillNo = reader["ACCOUNTBILLNO"] as string ?? string.Empty,
                                BillDate = reader["BILLDATE"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["BILLDATE"]),
                                PartyName = reader["PARTY_NAME"] as string ?? string.Empty,
                                BillDescription = reader["BILL_DESCRIPTION"] as string ?? string.Empty,
                                GrossAmount = ToDecimal(reader["GROSS_AMOUNT"]),
                                DeductionAmount = ToDecimal(reader["DEDUCTION_AMOUNT"]),
                                NetAmount = ToDecimal(reader["NET_AMOUNT"]),
                                Remarks = reader["REMARKS"] as string ?? string.Empty,
                                EnteredBy = reader["ENTBY"] as string ?? string.Empty,
                                EnteredDate = reader["ENTDT"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["ENTDT"]),
                            });
                        }
                    }
                }
            }
            return rows;
        }

        private static bool HasActiveMapping(OracleConnection conn, OracleTransaction tx, int ulbCode, long regBillNo, string accountBillNo)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM ABAS.BMT_BILL_MAPPING
                WHERE ULB_CODE = :ulb_code
                  AND REGBILLNO = :regbillno
                  AND NVL(ACCOUNTBILLNO, '') = NVL(:accountbillno, '')
                  AND NVL(VALIDFLAG, 'y') = 'y'";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.Transaction = tx;
                cmd.BindByName = true;
                cmd.Parameters.Add("ulb_code", OracleDbType.Decimal).Value = ulbCode;
                cmd.Parameters.Add("regbillno", OracleDbType.Decimal).Value = regBillNo;
                cmd.Parameters.Add("accountbillno", OracleDbType.Varchar2).Value = accountBillNo ?? string.Empty;
                return ToInt(cmd.ExecuteScalar()) > 0;
            }
        }

        private static HashSet<string> GetTableColumns(OracleConnection conn, string tableName)
        {
            const string sql = @"
                SELECT COLUMN_NAME
                FROM ALL_TAB_COLUMNS
                WHERE OWNER = 'ABAS'
                  AND TABLE_NAME = :table_name";

            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("table_name", OracleDbType.Varchar2).Value = tableName;
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        set.Add(reader["COLUMN_NAME"] as string ?? string.Empty);
                }
            }
            return set;
        }

        private static string Pick(HashSet<string> columns, params string[] candidates)
        {
            return candidates.FirstOrDefault(candidate => columns.Contains(candidate));
        }

        private static int ToInt(object value)
        {
            return value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);
        }

        private static long ToLong(object value)
        {
            return value == null || value == DBNull.Value ? 0L : Convert.ToInt64(value);
        }

        private static decimal ToDecimal(object value)
        {
            return value == null || value == DBNull.Value ? 0m : Convert.ToDecimal(value);
        }

        private static string Trunc8(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "ERP";
            var trimmed = value.Trim().ToUpperInvariant();
            return trimmed.Length <= 8 ? trimmed : trimmed.Substring(0, 8);
        }
    }
}
