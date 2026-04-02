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
        ApiResponse<object> GetUserProfile(string userId);
        ApiResponse<object> ChangePassword(string userId, string oldPassword, string newPassword);
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

        public ApiResponse<object> GetUserProfile(string userId)
        {
            try
            {
                using (var conn = _connFactory.CreateUlberp())
                using (var cmd = new OracleCommand("SP_GET_USER_PROFILE", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("P_USER_ID", OracleDbType.Varchar2).Value = userId;
                    cmd.Parameters.Add("O_SUCCESS", OracleDbType.Decimal).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("O_MESSAGE", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("O_USER_DATA", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    var success = GetSuccessOutput(cmd.Parameters["O_SUCCESS"].Value);
                    var message = GetMessageOutput(cmd.Parameters["O_MESSAGE"].Value, "Unknown error");

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

                                if (table.Rows.Count > 0)
                                {
                                    var row = table.Rows[0];
                                    data = new
                                    {
                                        userId = GetCol(row, "USER_ID"),
                                        role = GetCol(row, "ROLE"),
                                        name = GetCol(row, "NAME"),
                                        status = GetCol(row, "STATUS"),
                                        bankId = GetCol(row, "BANK_ID"),
                                        bankName = GetCol(row, "BANK_NAME"),
                                        roleId = GetRoleIdOutput(row)
                                    };
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

        public ApiResponse<object> ChangePassword(string userId, string oldPassword, string newPassword)
        {
            try
            {
                return ExecuteChangePasswordProcedure("SP_CHANGE_USER_PASSWORD", userId, oldPassword, newPassword);
            }
            catch (OracleException ex)
            {
                // Fallback for schema visibility issues (PLS-00201)
                if (ex.Number == 6550 && ex.Message != null && ex.Message.IndexOf("SP_CHANGE_USER_PASSWORD", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    try
                    {
                        return ExecuteChangePasswordProcedure("ULBERP.SP_CHANGE_USER_PASSWORD", userId, oldPassword, newPassword);
                    }
                    catch (OracleException fallbackEx)
                    {
                        return ApiResponse<object>.CreateError("Database error: " + fallbackEx.Message, "DBERR");
                    }
                }

                return ApiResponse<object>.CreateError("Database error: " + ex.Message, "DBERR");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.CreateError("Internal server error: " + ex.Message, "INTERNAL");
            }
        }

        private ApiResponse<object> ExecuteChangePasswordProcedure(string procedureName, string userId, string oldPassword, string newPassword)
        {
            using (var conn = _connFactory.CreateUlberp())
            using (var cmd = new OracleCommand(procedureName, conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("P_USER_ID", OracleDbType.Varchar2).Value = userId;
                cmd.Parameters.Add("P_OLD_PASSWORD", OracleDbType.Varchar2).Value = oldPassword;
                cmd.Parameters.Add("P_NEW_PASSWORD", OracleDbType.Varchar2).Value = newPassword;
                cmd.Parameters.Add("O_SUCCESS", OracleDbType.Decimal).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("O_MESSAGE", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;

                conn.Open();
                cmd.ExecuteNonQuery();

                return new ApiResponse<object>
                {
                    Success = GetSuccessOutput(cmd.Parameters["O_SUCCESS"].Value),
                    Message = GetMessageOutput(cmd.Parameters["O_MESSAGE"].Value, "Unknown error"),
                    Data = null
                };
            }
        }

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
                    var success = GetSuccessOutput(cmd.Parameters["O_SUCCESS"].Value);
                    var message = GetMessageOutput(cmd.Parameters["O_MESSAGE"].Value, "Unknown error");
                    
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
                                    
                                    string bankName = null;
                                    if (row.Table.Columns.Contains("BANK_NAME") && row["BANK_NAME"] != DBNull.Value)
                                        bankName = row["BANK_NAME"].ToString();
                                    
                                    // Build response object with all available fields
                                    var responseData = new
                                    {
                                        userId = row["USER_ID"]?.ToString(),
                                        role = row["ROLE"]?.ToString(),
                                        name = row["NAME"]?.ToString(),
                                        status = row["STATUS"]?.ToString(),
                                        bankId = bankId,
                                        bankName = bankName,
                                        roleId = GetRoleIdOutput(row)
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

        private static bool GetSuccessOutput(object successParam)
        {
            if (successParam == null || successParam == DBNull.Value)
            {
                return false;
            }

            if (successParam is OracleDecimal)
            {
                return ((OracleDecimal)successParam).ToInt32() == 1;
            }

            return Convert.ToInt32(successParam) == 1;
        }

        private static string GetMessageOutput(object messageParam, string fallback)
        {
            if (messageParam == null || messageParam == DBNull.Value)
            {
                return fallback;
            }

            return Convert.ToString(messageParam);
        }

        private static string GetCol(DataRow row, string col)
        {
            if (!row.Table.Columns.Contains(col) || row[col] == DBNull.Value)
            {
                return null;
            }

            return row[col].ToString();
        }

        private static int? GetRoleIdOutput(DataRow row)
        {
            if (!row.Table.Columns.Contains("ROLE_ID") || row["ROLE_ID"] == DBNull.Value)
            {
                return null;
            }

            var roleId = row["ROLE_ID"];
            return roleId is OracleDecimal
                ? ((OracleDecimal)roleId).ToInt32()
                : Convert.ToInt32(roleId);
        }
    }
}
