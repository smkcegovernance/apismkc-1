using System;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using SmkcApi.Models;

namespace SmkcApi.Repositories
{
    public interface IAuthRepository
    {
        ApiResponse<object> Bank_Login(string userId, string password);
        ApiResponse<object> Account_Login(string userId, string password);
        ApiResponse<object> Commissioner_Login(string userId, string password);
        ApiResponse<object> UnifiedLogin(string userId, string password);
    }

    public class AuthRepository : IAuthRepository
    {
        private readonly IOracleConnectionFactory _connFactory;
        public AuthRepository(IOracleConnectionFactory connFactory)
        {
            _connFactory = connFactory;
        }

        public ApiResponse<object> Bank_Login(string userId, string password)
            => ExecuteUlberp("SP_BANK_LOGIN", userId, password);

        public ApiResponse<object> Account_Login(string userId, string password)
            => ExecuteUlberp("SP_ACCOUNT_LOGIN", userId, password);

        public ApiResponse<object> Commissioner_Login(string userId, string password)
            => ExecuteUlberp("SP_COMMISSIONER_LOGIN", userId, password);

        public ApiResponse<object> UnifiedLogin(string userId, string password)
            => ExecuteUlberp("SP_UNIFIED_LOGIN", userId, password);

        private ApiResponse<object> ExecuteUlberp(string spName, string userId, string password)
        {
            try
            {
                using (var conn = _connFactory.CreateUlberp())
                using (var cmd = new OracleCommand(spName, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    
                    // Parameters matching stored procedure signature
                    cmd.Parameters.Add("P_USER_ID", OracleDbType.Varchar2).Value = userId;
                    cmd.Parameters.Add("P_PASSWORD", OracleDbType.Varchar2).Value = password;
                    cmd.Parameters.Add("O_SUCCESS", OracleDbType.Decimal).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("O_MESSAGE", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("O_USER_DATA", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    // Handle Oracle-specific data types properly
                    var successParam = cmd.Parameters["O_SUCCESS"].Value;
                    bool success = false;
                    
                    if (successParam != null && successParam != DBNull.Value)
                    {
                        // Handle OracleDecimal properly
                        if (successParam is OracleDecimal)
                        {
                            success = ((OracleDecimal)successParam).ToInt32() == 1;
                        }
                        else
                        {
                            success = Convert.ToInt32(successParam) == 1;
                        }
                    }
                    
                    var messageParam = cmd.Parameters["O_MESSAGE"].Value;
                    string message = messageParam != null && messageParam != DBNull.Value 
                        ? Convert.ToString(messageParam) 
                        : "Unknown error";
                    
                    object data = null;
                    
                    var cursorParam = cmd.Parameters["O_USER_DATA"];
                    if (cursorParam != null && cursorParam.Value is OracleRefCursor)
                    {
                        using (var reader = ((OracleRefCursor)cursorParam.Value).GetDataReader())
                        {
                            if (reader.HasRows)
                            {
                                var table = new DataTable();
                                table.Load(reader);
                                
                                // Convert DataTable to a more friendly object
                                if (table.Rows.Count > 0)
                                {
                                    var row = table.Rows[0];

                                    string bankId = null;
                                    if (row.Table.Columns.Contains("BANK_ID") && row["BANK_ID"] != DBNull.Value)
                                        bankId = row["BANK_ID"].ToString();
                                    else if (row.Table.Columns.Contains("BANKID") && row["BANKID"] != DBNull.Value)
                                        bankId = row["BANKID"].ToString();
                                    else if (row.Table.Columns.Contains("BANK_CODE") && row["BANK_CODE"] != DBNull.Value)
                                        bankId = row["BANK_CODE"].ToString();
                                    
                                    // Build response object with all available fields
                                    var responseData = new
                                    {
                                        userId = row["USER_ID"]?.ToString(),
                                        role = row["ROLE"]?.ToString(),
                                        name = row["NAME"]?.ToString(),
                                        status = row["STATUS"]?.ToString(),
                                        bankId = bankId,
                                        roleId = row.Table.Columns.Contains("ROLE_ID") && row["ROLE_ID"] != DBNull.Value
                                            ? (row["ROLE_ID"] is OracleDecimal 
                                                ? ((OracleDecimal)row["ROLE_ID"]).ToInt32() 
                                                : Convert.ToInt32(row["ROLE_ID"]))
                                            : (int?)null
                                    };
                                    
                                    data = responseData;
                                }
                            }
                        }
                    }
                    
                    return new ApiResponse<object> 
                    { 
                        Success = success, 
                        Message = message, 
                        Data = data 
                    };
                }
            }
            catch (OracleException ex)
            {
                return ApiResponse<object>.CreateError("Database error: " + ex.Message, "DBERR");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.CreateError("Internal server error: " + ex.Message, "INTERNAL");
            }
        }
    }
}
