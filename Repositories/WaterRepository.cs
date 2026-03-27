using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using SmkcApi.Models;
using SmkcApi.Utils; // <-- for LegacyIndicConverter

namespace SmkcApi.Repositories
{
    public interface IWaterRepository
    {
        Task<CustomerDto> GetCustomerAsync(string consumerNo);
        Task<BalanceDto> GetBalanceAsync(string consumerNo, string from, string to);
        Task<CollectionPostResponse> GetReceiptByBankTxnAsync(string bankTxnId);
        Task<CollectionPostResponse> PostCollectionAsync(CollectionPostRequest req, string idemKey);
        Task<ProcBillDto> GetBillViaProcAsync(string consumerNo);
        Task<List<ConnectionBalanceMobileDto>> GetConnectionBalanceWithMobileAsync(
           long connectionNo = 0, string wardCode = "0", string divCode = "0");
        Task<List<WaterBillSmsDto>> GetWaterBillSmsDataAsync(
           long connectionNo = 0, string wardCode = "0", string divCode = "0");
        Task LogSmsToOracleAsync(
            string connectionNo,
            string mobileNo,
            string message,
            string responseCode,
            string responseMessage,
            string providerResponse);
    }

    public class WaterRepository : IWaterRepository
    {
        // If the proc is in another schema, use "SCHEMA.GET_CUSTOMER_API"
        private const string PROC_NAME = "GET_CUSTOMER_API";

        private readonly IOracleConnectionFactory _factory;
        public WaterRepository(IOracleConnectionFactory factory) { _factory = factory; }

        public async Task<List<ConnectionBalanceMobileDto>> GetConnectionBalanceWithMobileAsync(
           long connectionNo = 0, string wardCode = "0", string divCode = "0")
        {
            var results = new List<ConnectionBalanceMobileDto>();

            using (var conn = _factory.Create())
            using (var cmd = conn.CreateCommand())
            {
                cmd.BindByName = true;
                cmd.CommandText = "GET_CONNECTION_BALANCE_DATA_WITH_MOBILE"; // if schema: "SCHEMA.GET_CONNECTION_BALANCE_DATA_WITH_MOBILE"
                cmd.CommandType = System.Data.CommandType.StoredProcedure;

                cmd.Parameters.Add("p_connection_no", OracleDbType.Int64).Value = connectionNo;
                cmd.Parameters.Add("p_ward_code", OracleDbType.Varchar2).Value = wardCode;
                cmd.Parameters.Add("p_div_code", OracleDbType.Varchar2).Value = divCode;
                var outCursor = new OracleParameter("p_result", OracleDbType.RefCursor, System.Data.ParameterDirection.Output);
                cmd.Parameters.Add(outCursor);

                await conn.OpenAsync();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        // Skip rows without mobile
                        var mobile = reader["mobile_number"] == DBNull.Value ? null : reader["mobile_number"].ToString();
                        if (string.IsNullOrWhiteSpace(mobile))
                        {
                            // Optionally still include but mark mobile null
                        }

                        DateTime queryDate = DateTime.Now;
                        if (!(reader["query_date"] is DBNull))
                            queryDate = Convert.ToDateTime(reader["query_date"], CultureInfo.InvariantCulture);

                        results.Add(new ConnectionBalanceMobileDto
                        {
                            ConnectionNumber = reader["connection_number"]?.ToString(),
                            WardCode = reader["ward_code"]?.ToString(),
                            WardName = reader["ward_name"]?.ToString(),
                            DivCode = reader["div_code"]?.ToString(),
                            DivName = reader["div_name"]?.ToString(),
                            MobileNumber = mobile,
                            CustomerNumber = reader["customer_number"]?.ToString(),
                            TotalBalance = reader["total_balance"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["total_balance"]),
                            DiscountAmount = reader["discount_amount"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["discount_amount"]),
                            AfterDiscountBalance = reader["after_discount_balance"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["after_discount_balance"]),
                            Status = reader["status"]?.ToString(),
                            QueryType = reader["query_type"]?.ToString(),
                            SearchCriteria = reader["search_criteria"]?.ToString(),
                            QueryDate = queryDate
                        });
                    }
                }
            }

            return results;
        }
        // ---------------------------
        // Master / Balance (existing)
        // ---------------------------
        public Task<CustomerDto> GetCustomerAsync(string consumerNo)
        {
            const string sql = @"
                SELECT CONSUMER_NO, NAME, ADDRESS, STATUS, MOBILE,
                       LAST_PAYMENT_DATE, LAST_PAYMENT_AMOUNT
                  FROM WATER_CUSTOMERS
                 WHERE CONSUMER_NO = :p_consumer_no";

            using (var conn = _factory.Create())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("p_consumer_no", consumerNo);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return Task.FromResult<CustomerDto>(null);

                    // Convert legacy name/address if needed
                    var rawName = dr["NAME"].ToString();
                    var rawAddr = dr["ADDRESS"].ToString();
                    var nameUni = ConvertIfLegacy(rawName);
                    var addrUni = ConvertIfLegacy(rawAddr);

                    var dto = new CustomerDto
                    {
                        ConsumerNo = dr["CONSUMER_NO"].ToString(),
                        Name = nameUni,
                        Address = addrUni,
                        Status = dr["STATUS"].ToString(),
                        Mobile = dr["MOBILE"].ToString(),
                        LastPaymentDate = dr["LAST_PAYMENT_DATE"] == DBNull.Value ? null : Convert.ToDateTime(dr["LAST_PAYMENT_DATE"]).ToString("yyyy-MM-dd"),
                        LastPaymentAmount = dr["LAST_PAYMENT_AMOUNT"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(dr["LAST_PAYMENT_AMOUNT"])
                    };
                    return Task.FromResult(dto);
                }
            }
        }

        public Task<BalanceDto> GetBalanceAsync(string consumerNo, string from, string to)
        {
            const string sql = @"
                SELECT ARREARS, CURRENT_DUE, LATE_FEE, (ARREARS+CURRENT_DUE+LATE_FEE) TOTAL_DUE,
                       TO_CHAR(SYSDATE, 'YYYY-MM-DD') AS_OF_DATE
                  FROM WATER_BALANCE_VIEW
                 WHERE CONSUMER_NO = :p_consumer_no";

            using (var conn = _factory.Create())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("p_consumer_no", consumerNo);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return Task.FromResult(new BalanceDto());
                    var dto = new BalanceDto
                    {
                        Arrears = dr["ARREARS"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["ARREARS"]),
                        Current = dr["CURRENT_DUE"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["CURRENT_DUE"]),
                        LateFee = dr["LATE_FEE"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["LATE_FEE"]),
                        TotalDue = dr["TOTAL_DUE"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["TOTAL_DUE"]),
                        AsOfDate = dr["AS_OF_DATE"].ToString()
                    };
                    return Task.FromResult(dto);
                }
            }
        }

        public Task<CollectionPostResponse> GetReceiptByBankTxnAsync(string bankTxnId)
        {
            const string sql = @"
                SELECT RECEIPT_NO, CONSUMER_NO, AMOUNT, POSTED_AT, BANK_TXN_ID, BALANCE_AFTER
                  FROM WATER_COLLECTIONS
                 WHERE BANK_TXN_ID = :p_bank_txn_id";

            using (var conn = _factory.Create())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("p_bank_txn_id", bankTxnId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return Task.FromResult<CollectionPostResponse>(null);
                    var dto = new CollectionPostResponse
                    {
                        ReceiptNo = dr["RECEIPT_NO"].ToString(),
                        ConsumerNo = dr["CONSUMER_NO"].ToString(),
                        PostedAmount = Convert.ToDecimal(dr["AMOUNT"]),
                        PostedAt = Convert.ToDateTime(dr["POSTED_AT"]).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        BankTxnId = dr["BANK_TXN_ID"].ToString(),
                        BalanceAfter = Convert.ToDecimal(dr["BALANCE_AFTER"])
                    };
                    return Task.FromResult(dto);
                }
            }
        }

        public async Task<CollectionPostResponse> PostCollectionAsync(CollectionPostRequest req, string idemKey)
        {
            using (var conn = _factory.Create())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    var insert = new OracleCommand(@"
                        INSERT INTO WATER_COLLECTIONS
                          (RECEIPT_NO, CONSUMER_NO, AMOUNT, POSTED_AT, BANK_TXN_ID, UTR_NO, MODE, NARRATION, IDEMPOTENCY_KEY)
                        VALUES
                          (:p_receipt_no, :p_consumer_no, :p_amount, SYSTIMESTAMP, :p_bank_txn_id, :p_utr, :p_mode, :p_narr, :p_idem_key)",
                        conn);
                    insert.BindByName = true;
                    var receiptNo = GenerateReceipt(conn);
                    insert.Parameters.Add("p_receipt_no", receiptNo);
                    insert.Parameters.Add("p_consumer_no", req.ConsumerNo);
                    insert.Parameters.Add("p_amount", req.Amount);
                    insert.Parameters.Add("p_bank_txn_id", req.BankTxnId);
                    insert.Parameters.Add("p_utr", (object)req.UtrNo ?? DBNull.Value);
                    insert.Parameters.Add("p_mode", (object)req.PaymentMode ?? "UNKNOWN");
                    insert.Parameters.Add("p_narr", (object)req.Narration ?? DBNull.Value);
                    insert.Parameters.Add("p_idem_key", (object)idemKey ?? DBNull.Value);
                    insert.ExecuteNonQuery();

                    var upd = new OracleCommand(@"
                        UPDATE WATER_LEDGER
                           SET PAID_AMOUNT = NVL(PAID_AMOUNT,0) + :p_amount
                         WHERE CONSUMER_NO = :p_consumer_no", conn);
                    upd.BindByName = true;
                    upd.Parameters.Add("p_amount", req.Amount);
                    upd.Parameters.Add("p_consumer_no", req.ConsumerNo);
                    upd.ExecuteNonQuery();

                    var bal = await GetBalanceAsync(req.ConsumerNo, null, null);
                    tx.Commit();

                    return new CollectionPostResponse
                    {
                        ReceiptNo = receiptNo,
                        ConsumerNo = req.ConsumerNo,
                        PostedAmount = req.Amount,
                        PostedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        BankTxnId = req.BankTxnId,
                        BalanceAfter = bal.TotalDue
                    };
                }
            }
        }

        private string GenerateReceipt(OracleConnection conn)
        {
            using (var cmd = new OracleCommand("SELECT 'WT-' || TO_CHAR(SYSDATE,'YYYY') || '-' || LPAD(WATER_RCPT_SEQ.NEXTVAL,6,'0') FROM DUAL", conn))
            {
                var o = cmd.ExecuteScalar();
                return o.ToString();
            }
        }

        // ---------------------------
        // Bill Fetch via Stored Proc
        // ---------------------------
        public Task<ProcBillDto> GetBillViaProcAsync(string consumerNo)
        {
            if (string.IsNullOrWhiteSpace(consumerNo))
                return Task.FromResult<ProcBillDto>(null);

            long connNumber;
            if (!long.TryParse(consumerNo, out connNumber))
                throw new ArgumentException("Invalid connection number");

            using (var conn = _factory.Create())
            using (var cmd = conn.CreateCommand())
            {
                cmd.BindByName = true;
                cmd.CommandText = PROC_NAME; // or "SCHEMA.GET_CUSTOMER_API"
                cmd.CommandType = System.Data.CommandType.StoredProcedure;

                cmd.Parameters.Add("p_connection_no", OracleDbType.Int64).Value = connNumber;
                var outCursor = new OracleParameter("p_result", OracleDbType.RefCursor, System.Data.ParameterDirection.Output);
                cmd.Parameters.Add(outCursor);

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.HasRows) return Task.FromResult<ProcBillDto>(null);
                    if (!reader.Read()) return Task.FromResult<ProcBillDto>(null);

                    // Build a case-insensitive column map once
                    var ord = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < reader.FieldCount; i++)
                        ord[reader.GetName(i)] = i;

                    // helpers (case-insensitive)
                    Func<string, int> Ord = col => ord.ContainsKey(col) ? ord[col] : -1;
                    Func<string, bool> Has = col => { var i = Ord(col); return i >= 0 && !reader.IsDBNull(i); };
                    Func<string, string> S = col => { var i = Ord(col); return i >= 0 && !reader.IsDBNull(i) ? reader.GetValue(i).ToString() : null; };
                    Func<string, decimal> D = col => { var i = Ord(col); return i >= 0 && !reader.IsDBNull(i) ? Convert.ToDecimal(reader.GetValue(i)) : 0m; };
                    Func<string, int> I = col => { var i = Ord(col); return i >= 0 && !reader.IsDBNull(i) ? Convert.ToInt32(reader.GetValue(i)) : 0; };
                    Func<string, string> Dt = col =>
                    {
                        var i = Ord(col);
                        if (i < 0 || reader.IsDBNull(i)) return null;
                        var dt = Convert.ToDateTime(reader.GetValue(i), CultureInfo.InvariantCulture);
                        if (col.Equals("QueryDateTime", StringComparison.OrdinalIgnoreCase))
                            return dt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
                        return dt.ToString("yyyy-MM-dd");
                    };

                    var rawName = S("CustomerName");
                    var rawAddr = S("CustomerAddress");

                    var nameUni = NetIsmConverter.ConvertIfNeeded(rawName);
                    var addrUni = NetIsmConverter.ConvertIfNeeded(rawAddr);


                    var dto = new ProcBillDto
                    {
                        ConnectionNumber = S("ConnectionNumber"),
                        CustomerName = nameUni,
                        OccupantName = S("OccupantName"),
                        CustomerAddress = addrUni,

                        WardCode = S("WardCode"),
                        WardName = S("WardName"),
                        DivisionCode = I("DivisionCode"),
                        DivisionName = S("DivisionName"),

                        CurrentBalance = D("CurrentBalance"),
                        MeterRentBalance = D("MeterRentBalance"),
                        TotalBalance = D("TotalBalance"),
                        InterestBalance = D("InterestBalance"),
                        LastPaymentAmount = D("LastPaymentAmount"),

                        LastBillDueDate = Dt("LastBillDueDate"),
                        LastBillDate = Dt("LastBillDate"),
                        LastPaymentDate = Dt("LastPaymentDate"),
                        ConnectionDate = Dt("ConnectionDate"),

                        PaymentStatus = S("PaymentStatus"),
                        DaysOverdue = I("DaysOverdue"),
                        FinancialYear = S("FinancialYear"),
                        MeterNumber = S("MeterNumber"),

                        ApiStatus = S("ApiStatus"),
                        ApiMessage = S("ApiMessage"),
                        QueryDateTime = Dt("QueryDateTime") ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    };

                    return Task.FromResult(dto);
                }
            }
        }
        // ------------------------------------
        // Log SMS Activity to Oracle
        // ------------------------------------
        public async Task LogSmsToOracleAsync(
            string connectionNo,
            string mobileNo,
            string message,
            string responseCode,
            string responseMessage,
            string providerResponse)
        {
            try
            {
                using (var conn = _factory.CreateWS())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "PROC_LOG_SMS_ACTIVITY";
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.BindByName = true;

                    cmd.Parameters.Add("p_connection_no", OracleDbType.Decimal)
                        .Value = string.IsNullOrEmpty(connectionNo) ? (object)DBNull.Value : Convert.ToDecimal(connectionNo);
                    cmd.Parameters.Add("p_mobile_no", OracleDbType.Varchar2, mobileNo, System.Data.ParameterDirection.Input);
                    cmd.Parameters.Add("p_message_text", OracleDbType.Varchar2, message, System.Data.ParameterDirection.Input);
                    cmd.Parameters.Add("p_response_code", OracleDbType.Varchar2, responseCode, System.Data.ParameterDirection.Input);
                    cmd.Parameters.Add("p_response_message", OracleDbType.Varchar2, responseMessage, System.Data.ParameterDirection.Input);
                    cmd.Parameters.Add("p_provider_response", OracleDbType.Clob, providerResponse, System.Data.ParameterDirection.Input);
                    cmd.Parameters.Add("p_sent_by", OracleDbType.Varchar2, "API", System.Data.ParameterDirection.Input);

                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();

                    System.Diagnostics.Trace.TraceInformation(
                        $"[WaterRepository] SMS Log saved for {mobileNo} (Conn {connectionNo})");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[WaterRepository] Failed to log SMS: {ex}");
            }
        }

        // New method to get water bill SMS data with customer name and due date
        public async Task<List<WaterBillSmsDto>> GetWaterBillSmsDataAsync(
            long connectionNo = 0, string wardCode = "0", string divCode = "0")
        {
            var results = new List<WaterBillSmsDto>();

            using (var conn = _factory.CreateWS())
            using (var cmd = conn.CreateCommand())
            {
                cmd.BindByName = true;
                cmd.CommandText = "PROC_GET_WS_SMS_PAYLOAD"; // stored procedure from your screenshot
                cmd.CommandType = System.Data.CommandType.StoredProcedure;

                cmd.Parameters.Add("p_result", OracleDbType.RefCursor, System.Data.ParameterDirection.Output);
                cmd.Parameters.Add("p_current_finyr", OracleDbType.Varchar2).Value = "2025-2026"; // or get from config
                cmd.Parameters.Add("p_current_bc_code", OracleDbType.Varchar2).Value = "18"; // or get from config
                cmd.Parameters.Add("p_payment_url", OracleDbType.Varchar2).Value = "https://tinyurl.com/ye89wuk3";
                cmd.Parameters.Add("p_ws_connno", OracleDbType.Int64).Value = connectionNo;
                cmd.Parameters.Add("p_ward_code", OracleDbType.Varchar2).Value = wardCode;
                cmd.Parameters.Add("p_div_code", OracleDbType.Varchar2).Value = divCode;
                cmd.Parameters.Add("p_only_with_mobile", OracleDbType.Varchar2).Value = "y";

                await conn.OpenAsync();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var mobile = reader["mobile_number"] == DBNull.Value ? null : reader["mobile_number"].ToString();
                        if (string.IsNullOrWhiteSpace(mobile))
                            continue; // Skip rows without mobile

                        var customerName = reader["customer_name"] == DBNull.Value ? "" : reader["customer_name"].ToString();
                        
                        // Format due date as dd/MM/yyyy without time
                        string dueDate = "";
                        if (!(reader["due_date"] is DBNull))
                        {
                            var dueDateValue = Convert.ToDateTime(reader["due_date"], CultureInfo.InvariantCulture);
                            dueDate = dueDateValue.ToString("dd/MM/yyyy");
                        }
                        
                        results.Add(new WaterBillSmsDto
                        {
                            ConnectionNumber = reader["connection_no"]?.ToString(),
                            CustomerName = NetIsmConverter.ConvertIfNeeded(customerName),
                            MobileNumber = mobile,
                            TotalAmount = reader["total_amount"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["total_amount"]),
                            DueDate = dueDate,
                            PaymentUrl = reader["payment_url"]?.ToString()
                        });
                    }
                }
            }

            return results;
        }

        // Detects legacy mojibake (non-ASCII and not already Devanagari), then converts to Unicode Marathi
        private static string ConvertIfLegacy(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;

            // If it already contains Devanagari, return as is
            foreach (var ch in s)
                if (ch >= '\u0900' && ch <= '\u097F') return s;

            // If it has extended Latin/control glyphs (typical of ISM), try converting
            bool hasHighLatin = false;
            foreach (var ch in s)
            {
                if (ch > 0x7F && !(ch >= '\u0900' && ch <= '\u097F'))
                {
                    hasHighLatin = true;
                    break;
                }
            }
            return hasHighLatin ? DvbwConverter.Convert(s) : s;
        }
    }

    // ======= DB DTOs (repo projections) =======
    public class CustomerDto
    {
        public string ConsumerNo { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Status { get; set; }
        public string Mobile { get; set; }
        public string LastPaymentDate { get; set; }
        public decimal? LastPaymentAmount { get; set; }
    }

    public class BalanceDto
    {
        public decimal Arrears { get; set; }
        public decimal Current { get; set; }
        public decimal LateFee { get; set; }
        public decimal TotalDue { get; set; }
        public string AsOfDate { get; set; }
    }

    // Returned by GET_CUSTOMER_API; service maps to API BillFetchResponse
    public class ProcBillDto
    {
        public string ConnectionNumber { get; set; }
        public string CustomerName { get; set; }
        public string OccupantName { get; set; }
        public string CustomerAddress { get; set; }

        public string WardCode { get; set; }
        public string WardName { get; set; }
        public int DivisionCode { get; set; }
        public string DivisionName { get; set; }

        public decimal CurrentBalance { get; set; }
        public decimal MeterRentBalance { get; set; }
        public decimal TotalBalance { get; set; }
        public decimal InterestBalance { get; set; }
        public decimal LastPaymentAmount { get; set; }

        public string LastBillDueDate { get; set; }
        public string LastBillDate { get; set; }
        public string LastPaymentDate { get; set; }
        public string ConnectionDate { get; set; }

        public string PaymentStatus { get; set; }
        public int DaysOverdue { get; set; }
        public string FinancialYear { get; set; }
        public string MeterNumber { get; set; }

        public string ApiStatus { get; set; }
        public string ApiMessage { get; set; }
        public string QueryDateTime { get; set; }
    }
}
