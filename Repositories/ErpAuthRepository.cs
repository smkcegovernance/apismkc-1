using System;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using SmkcApi.Models;

namespace SmkcApi.Repositories
{
    public interface IErpAuthRepository
    {
        ApiResponse<object> ErpLogin(string userId, string password, string ipAddress);
        ApiResponse<object> ErpGetProfile(string userId);
        ApiResponse<object> ErpChangePassword(string userId, string oldPassword, string newPassword);
        ApiResponse<object> ErpGetLockedUsers(string adminUserId);
        ApiResponse<object> ErpUnlockUser(string adminUserId, string targetUserId);
    }

    public class ErpAuthRepository : IErpAuthRepository
    {
        private readonly IOracleConnectionFactory _connFactory;

        public ErpAuthRepository(IOracleConnectionFactory connFactory)
        {
            _connFactory = connFactory;
        }

        public ApiResponse<object> ErpLogin(string userId, string password, string ipAddress)
        {
            try
            {
                using (var conn = _connFactory.CreateUlberp())
                using (var cmd = new OracleCommand("SP_ERP_USER_LOGIN", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("P_USER_ID",  OracleDbType.Varchar2).Value  = userId;
                    cmd.Parameters.Add("P_PASSWORD", OracleDbType.Varchar2).Value  = password;
                    cmd.Parameters.Add("O_SUCCESS",  OracleDbType.Decimal).Direction   = ParameterDirection.Output;
                    cmd.Parameters.Add("O_MESSAGE",  OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
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
                                        userId     = GetCol(row, "USER_ID"),
                                        name       = GetCol(row, "EMP_NAME"),
                                        status     = GetCol(row, "USER_STATUS"),
                                        validFrom  = GetDateCol(row, "USER_FROM"),
                                        validTo    = GetDateCol(row, "USER_TO"),
                                        roleId     = GetRoleId(row),
                                        role       = GetCol(row, "ROLE_NAME"),
                                    };
                                }
                            }
                        }
                    }

                    return new ApiResponse<object>
                    {
                        Success = success,
                        Message = message,
                        Data    = data
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

        public ApiResponse<object> ErpGetProfile(string userId)
        {
            try
            {
                using (var conn = _connFactory.CreateUlberp())
                using (var cmd = new OracleCommand("SP_ERP_GET_PROFILE", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("P_USER_ID",   OracleDbType.Varchar2).Value = userId;
                    cmd.Parameters.Add("O_SUCCESS",   OracleDbType.Decimal).Direction   = ParameterDirection.Output;
                    cmd.Parameters.Add("O_MESSAGE",   OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
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
                                        userId     = GetCol(row, "USER_ID"),
                                        name       = GetCol(row, "EMP_NAME"),
                                        status     = GetCol(row, "USER_STATUS"),
                                        validFlag  = GetCol(row, "USER_VIFLAG"),
                                        locked     = GetCol(row, "USER_LOCK"),
                                        validFrom  = GetDateCol(row, "USER_FROM"),
                                        validTo    = GetDateCol(row, "USER_TO"),
                                        roleId     = GetRoleId(row),
                                        role       = GetCol(row, "ROLE_NAME"),
                                    };
                                }
                            }
                        }
                    }

                    return new ApiResponse<object>
                    {
                        Success = success,
                        Message = message,
                        Data    = data
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

        public ApiResponse<object> ErpChangePassword(string userId, string oldPassword, string newPassword)
        {
            try
            {
                using (var conn = _connFactory.CreateUlberp())
                using (var cmd = new OracleCommand("SP_ERP_CHANGE_PASSWORD", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("P_USER_ID",       OracleDbType.Varchar2).Value = userId;
                    cmd.Parameters.Add("P_OLD_PASSWORD",  OracleDbType.Varchar2).Value = oldPassword;
                    cmd.Parameters.Add("P_NEW_PASSWORD",  OracleDbType.Varchar2).Value = newPassword;
                    cmd.Parameters.Add("O_SUCCESS",       OracleDbType.Decimal).Direction   = ParameterDirection.Output;
                    cmd.Parameters.Add("O_MESSAGE",       OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    return new ApiResponse<object>
                    {
                        Success = GetSuccessOutput(cmd.Parameters["O_SUCCESS"].Value),
                        Message = GetMessageOutput(cmd.Parameters["O_MESSAGE"].Value, "Unknown error"),
                        Data    = null
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

        // ── Helpers ───────────────────────────────────────────────────────────

        private static bool GetSuccessOutput(object raw)
        {
            if (raw == null || raw is DBNull) return false;
            if (raw is OracleDecimal od) return od.IsNull ? false : od.ToInt32() == 1;
            return Convert.ToInt32(raw) == 1;
        }

        private static string GetMessageOutput(object raw, string fallback)
        {
            if (raw == null || raw is DBNull) return fallback;
            if (raw is OracleString os) return os.IsNull ? fallback : os.Value;
            var s = raw.ToString();
            return string.IsNullOrEmpty(s) ? fallback : s;
        }

        private static string GetCol(DataRow row, string col)
        {
            if (!row.Table.Columns.Contains(col)) return null;
            var v = row[col];
            if (v == null || v is DBNull) return null;
            return v.ToString();
        }

        private static string GetDateCol(DataRow row, string col)
        {
            if (!row.Table.Columns.Contains(col)) return null;
            var v = row[col];
            if (v == null || v is DBNull) return null;
            if (v is DateTime dt) return dt.ToString("dd-MMM-yyyy");
            return v.ToString();
        }

        private static int GetRoleId(DataRow row)
        {
            if (!row.Table.Columns.Contains("ROLE_ID")) return 0;
            var v = row["ROLE_ID"];
            if (v == null || v is DBNull) return 0;
            if (v is OracleDecimal od) return od.IsNull ? 0 : od.ToInt32();
            int result;
            return int.TryParse(v.ToString(), out result) ? result : 0;
        }

        // ── User lock management ──────────────────────────────────────────────

        public ApiResponse<object> ErpGetLockedUsers(string adminUserId)
        {
            try
            {
                using (var conn = _connFactory.CreateUlberp())
                using (var cmd = new OracleCommand("SP_ADMIN_GET_LOCKED_USERS", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("P_ADMIN_ID", OracleDbType.Varchar2).Value = adminUserId;
                    cmd.Parameters.Add("O_SUCCESS",  OracleDbType.Decimal).Direction   = ParameterDirection.Output;
                    cmd.Parameters.Add("O_MESSAGE",  OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("O_DATA",     OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    var success = GetSuccessOutput(cmd.Parameters["O_SUCCESS"].Value);
                    var message = GetMessageOutput(cmd.Parameters["O_MESSAGE"].Value, "Unknown error");

                    if (!success)
                        return ApiResponse<object>.CreateError(message, "ADMIN");

                    var users = new System.Collections.Generic.List<object>();
                    var cursorParam = cmd.Parameters["O_DATA"];
                    if (cursorParam != null && cursorParam.Value is OracleRefCursor)
                    {
                        using (var reader = ((OracleRefCursor)cursorParam.Value).GetDataReader())
                        {
                            var table = new DataTable();
                            table.Load(reader);
                            foreach (DataRow row in table.Rows)
                            {
                                users.Add(new
                                {
                                    userId      = GetCol(row, "USER_ID"),
                                    name        = GetCol(row, "USER_NAME"),
                                    status      = GetCol(row, "USER_STATUS"),
                                    locked      = GetCol(row, "USER_LOCK"),
                                    validFrom   = GetDateCol(row, "USER_FROM"),
                                    validTo     = GetDateCol(row, "USER_TO"),
                                    lastAttempt = GetDateTimeCol(row, "LAST_ATTEMPT"),
                                });
                            }
                        }
                    }

                    return new ApiResponse<object> { Success = true, Message = message, Data = users };
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

        public ApiResponse<object> ErpUnlockUser(string adminUserId, string targetUserId)
        {
            try
            {
                using (var conn = _connFactory.CreateUlberp())
                using (var cmd = new OracleCommand("SP_ADMIN_UNLOCK_USER", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("P_ADMIN_ID",    OracleDbType.Varchar2).Value = adminUserId;
                    cmd.Parameters.Add("P_TARGET_USER", OracleDbType.Varchar2).Value = targetUserId;
                    cmd.Parameters.Add("O_SUCCESS",     OracleDbType.Decimal).Direction   = ParameterDirection.Output;
                    cmd.Parameters.Add("O_MESSAGE",     OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    return new ApiResponse<object>
                    {
                        Success = GetSuccessOutput(cmd.Parameters["O_SUCCESS"].Value),
                        Message = GetMessageOutput(cmd.Parameters["O_MESSAGE"].Value, "Unknown error"),
                        Data    = null
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

        private static string GetDateTimeCol(DataRow row, string col)
        {
            if (!row.Table.Columns.Contains(col)) return null;
            var v = row[col];
            if (v == null || v is DBNull) return null;
            if (v is DateTime dt) return dt.ToString("dd-MMM-yyyy HH:mm");
            return v.ToString();
        }
    }
}
