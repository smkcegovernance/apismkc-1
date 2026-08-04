using System;
using System.Collections.Generic;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using SmkcApi.Models;

namespace SmkcApi.Repositories
{
    public interface ISWRRepository
    {
        SWROperationResult<SWRSaveData> SaveRegistration(SWRRegistrationRequest request, int? regId);
        SWROperationResult<object>      ReplaceSchemes(int regId, IList<string> schemePairs);
        SWROperationResult<object>      GetRegistration(int? regId, string regNumber);
        SWROperationResult<object>      Search(string searchText, string status, string applicationMode);
        SWROperationResult<object>      UpdateStatus(int regId, string status, string remarks, int? operatorId);
        SWROperationResult<object>      ResolveRegistration(string identifier);
    }

    public class SWRRepository : ISWRRepository
    {
        private readonly IOracleConnectionFactory _connectionFactory;

        public SWRRepository(IOracleConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        // ----------------------------------------------------------------
        //  Save (INSERT or UPDATE)
        // ----------------------------------------------------------------
        public SWROperationResult<SWRSaveData> SaveRegistration(SWRRegistrationRequest req, int? regId)
        {
            try
            {
                using (var conn = _connectionFactory.Create())
                using (var cmd  = new OracleCommand("WCWC.SP_WCWC_SAVE_SINGLE_WOMEN_REG", conn))
                {
                    conn.Open();
                    cmd.CommandType = CommandType.StoredProcedure;

                    AddNullable(cmd, "P_REG_ID",           OracleDbType.Decimal,   regId);
                    AddNullable(cmd, "P_APPLICATION_MODE", OracleDbType.Varchar2,  req.ApplicationMode);
                    AddNullable(cmd, "P_FULL_NAME",        OracleDbType.NVarchar2, req.FullName);
                    AddNullable(cmd, "P_DOB",              OracleDbType.Date,      ParseDate(req.Dob));
                    AddNullable(cmd, "P_MOBILE_NUMBER",    OracleDbType.Varchar2,  req.MobileNumber);
                    AddNullable(cmd, "P_AADHAAR_NUMBER",   OracleDbType.Varchar2,  req.AadhaarNumber);
                    AddNullable(cmd, "P_DISTRICT",         OracleDbType.NVarchar2, req.District);
                    AddNullable(cmd, "P_TALUKA",           OracleDbType.NVarchar2, req.Taluka);
                    AddNullable(cmd, "P_VILLAGE",          OracleDbType.NVarchar2, req.Village);
                    AddNullable(cmd, "P_WARD",             OracleDbType.NVarchar2, req.Ward);
                    AddNullable(cmd, "P_WOMEN_CATEGORY",   OracleDbType.Varchar2,  req.WomenCategory);
                    AddNullable(cmd, "P_AGE_GROUP",        OracleDbType.Varchar2,  req.AgeGroup);
                    AddNullable(cmd, "P_OCCUPATION",       OracleDbType.NVarchar2, req.Occupation);
                    AddNullable(cmd, "P_ANNUAL_INCOME",    OracleDbType.Decimal,   ParseDecimal(req.AnnualIncome));
                    AddNullable(cmd, "P_MGNREGA_YEARS",    OracleDbType.Decimal,   ParseDecimal(req.MgnregaYears));
                    AddNullable(cmd, "P_CHILDREN_BELOW6",  OracleDbType.Decimal,   ParseDecimal(req.ChildrenBelow6));
                    AddNullable(cmd, "P_CHILDREN_6TO14",   OracleDbType.Decimal,   ParseDecimal(req.Children6To14));
                    AddNullable(cmd, "P_OTHER_INFO",       OracleDbType.NVarchar2, req.OtherInfo);
                    AddNullable(cmd, "P_PRIMARY_EARNER",   OracleDbType.NVarchar2, req.PrimaryEarner);
                    AddNullable(cmd, "P_OPERATOR_USER_ID", OracleDbType.Decimal,   req.OperatorUserId);

                    AddOut(cmd, "P_OUT_REG_ID",      OracleDbType.Decimal,   22);
                    AddOut(cmd, "P_OUT_REG_NUMBER",  OracleDbType.Varchar2,  30);
                    AddOut(cmd, "P_OUT_STATUS_CODE", OracleDbType.Varchar2,  30);
                    AddOut(cmd, "P_OUT_MESSAGE",     OracleDbType.NVarchar2, 500);

                    cmd.ExecuteNonQuery();

                    var statusCode = GetOutString(cmd, "P_OUT_STATUS_CODE");
                    var message    = GetOutString(cmd, "P_OUT_MESSAGE");

                    if (statusCode != "SUCCESS")
                        return Fail<SWRSaveData>(message, statusCode);

                    return new SWROperationResult<SWRSaveData>
                    {
                        Success = true,
                        Message = message,
                        Data    = new SWRSaveData
                        {
                            RegId              = GetOutInt(cmd, "P_OUT_REG_ID"),
                            RegistrationNumber = GetOutString(cmd, "P_OUT_REG_NUMBER")
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                return Fail<SWRSaveData>("Database error: " + ex.Message, "DB_ERROR");
            }
        }

        // ----------------------------------------------------------------
        //  Replace Schemes
        // ----------------------------------------------------------------
        public SWROperationResult<object> ReplaceSchemes(int regId, IList<string> schemePairs)
        {
            try
            {
                using (var conn = _connectionFactory.Create())
                using (var cmd  = new OracleCommand("WCWC.SP_WCWC_REPLACE_SWR_SCHEMES", conn))
                {
                    conn.Open();
                    cmd.CommandType = CommandType.StoredProcedure;

                    var p = cmd.Parameters.Add("P_REG_ID", OracleDbType.Decimal);
                    p.Value = regId;

                    // Pass schemes as comma-delimited CSV: "CODE|Y,CODE2|N,..."
                    // Procedure SP_WCWC_REPLACE_SWR_SCHEMES parses this internally.
                    var csvParam = cmd.Parameters.Add("P_SCHEMES_CSV", OracleDbType.Varchar2, 32767);
                    csvParam.Value = schemePairs != null && schemePairs.Count > 0
                        ? string.Join(",", schemePairs)
                        : string.Empty;

                    AddOut(cmd, "P_OUT_STATUS_CODE", OracleDbType.Varchar2,  30);
                    AddOut(cmd, "P_OUT_MESSAGE",     OracleDbType.NVarchar2, 500);

                    cmd.ExecuteNonQuery();

                    var statusCode = GetOutString(cmd, "P_OUT_STATUS_CODE");
                    var message    = GetOutString(cmd, "P_OUT_MESSAGE");

                    return statusCode == "SUCCESS"
                        ? new SWROperationResult<object> { Success = true, Message = message }
                        : Fail<object>(message, statusCode);
                }
            }
            catch (Exception ex)
            {
                return Fail<object>("Database error: " + ex.Message, "DB_ERROR");
            }
        }

        // ----------------------------------------------------------------
        //  Get Registration
        // ----------------------------------------------------------------
        public SWROperationResult<object> GetRegistration(int? regId, string regNumber)
        {
            try
            {
                using (var conn = _connectionFactory.Create())
                using (var cmd  = new OracleCommand("WCWC.SP_WCWC_GET_SINGLE_WOMEN_REG", conn))
                {
                    conn.Open();
                    cmd.CommandType = CommandType.StoredProcedure;

                    AddNullable(cmd, "P_REG_ID",     OracleDbType.Decimal, regId);
                    AddNullable(cmd, "P_REG_NUMBER",  OracleDbType.Varchar2, regNumber);

                    AddOutCursor(cmd, "P_OUT_CURSOR");
                    AddOutCursor(cmd, "P_OUT_SCH_CURSOR");
                    AddOut(cmd, "P_OUT_STATUS_CODE", OracleDbType.Varchar2,  30);
                    AddOut(cmd, "P_OUT_MESSAGE",     OracleDbType.NVarchar2, 500);

                    cmd.ExecuteNonQuery();

                    var statusCode = GetOutString(cmd, "P_OUT_STATUS_CODE");
                    var message    = GetOutString(cmd, "P_OUT_MESSAGE");

                    if (statusCode != "SUCCESS")
                        return Fail<object>(message, statusCode == "NOT_FOUND" ? "REG_NOT_FOUND" : statusCode);

                    var reg     = ReadSingleRow((OracleRefCursor)((OracleParameter)cmd.Parameters["P_OUT_CURSOR"]).Value);
                    var schemes = ReadRows((OracleRefCursor)((OracleParameter)cmd.Parameters["P_OUT_SCH_CURSOR"]).Value);

                    if (reg == null) return Fail<object>("Registration not found", "REG_NOT_FOUND");

                    reg["schemes"] = schemes;

                    return new SWROperationResult<object> { Success = true, Message = message, Data = reg };
                }
            }
            catch (Exception ex)
            {
                return Fail<object>("Database error: " + ex.Message, "DB_ERROR");
            }
        }

        // ----------------------------------------------------------------
        //  Search
        // ----------------------------------------------------------------
        public SWROperationResult<object> Search(string searchText, string status, string applicationMode)
        {
            try
            {
                using (var conn = _connectionFactory.Create())
                using (var cmd  = new OracleCommand("WCWC.SP_WCWC_SEARCH_SINGLE_WOMEN", conn))
                {
                    conn.Open();
                    cmd.CommandType = CommandType.StoredProcedure;

                    AddNullable(cmd, "P_SEARCH_TEXT",      OracleDbType.NVarchar2, searchText);
                    AddNullable(cmd, "P_STATUS",           OracleDbType.Varchar2,  status);
                    AddNullable(cmd, "P_APPLICATION_MODE", OracleDbType.Varchar2,  applicationMode);

                    AddOutCursor(cmd, "P_OUT_CURSOR");
                    AddOut(cmd, "P_OUT_STATUS_CODE", OracleDbType.Varchar2,  30);
                    AddOut(cmd, "P_OUT_MESSAGE",     OracleDbType.NVarchar2, 500);

                    cmd.ExecuteNonQuery();

                    var statusCode = GetOutString(cmd, "P_OUT_STATUS_CODE");
                    var message    = GetOutString(cmd, "P_OUT_MESSAGE");

                    if (statusCode != "SUCCESS")
                        return Fail<object>(message, statusCode);

                    var rows = ReadRows((OracleRefCursor)((OracleParameter)cmd.Parameters["P_OUT_CURSOR"]).Value);
                    return new SWROperationResult<object> { Success = true, Message = message, Data = rows };
                }
            }
            catch (Exception ex)
            {
                return Fail<object>("Database error: " + ex.Message, "DB_ERROR");
            }
        }

        // ----------------------------------------------------------------
        //  Update Status
        // ----------------------------------------------------------------
        public SWROperationResult<object> UpdateStatus(int regId, string status, string remarks, int? operatorId)
        {
            try
            {
                using (var conn = _connectionFactory.Create())
                using (var cmd  = new OracleCommand("WCWC.SP_WCWC_UPDATE_SWR_STATUS", conn))
                {
                    conn.Open();
                    cmd.CommandType = CommandType.StoredProcedure;

                    var p = cmd.Parameters.Add("P_REG_ID", OracleDbType.Decimal);
                    p.Value = regId;
                    AddNullable(cmd, "P_STATUS",      OracleDbType.Varchar2,  status);
                    AddNullable(cmd, "P_REMARKS",     OracleDbType.NVarchar2, remarks);
                    AddNullable(cmd, "P_OPERATOR_ID", OracleDbType.Decimal,   operatorId);

                    AddOut(cmd, "P_OUT_STATUS_CODE", OracleDbType.Varchar2,  30);
                    AddOut(cmd, "P_OUT_MESSAGE",     OracleDbType.NVarchar2, 500);

                    cmd.ExecuteNonQuery();

                    var statusCode = GetOutString(cmd, "P_OUT_STATUS_CODE");
                    var message    = GetOutString(cmd, "P_OUT_MESSAGE");

                    return statusCode == "SUCCESS"
                        ? new SWROperationResult<object> { Success = true, Message = message }
                        : Fail<object>(message, statusCode);
                }
            }
            catch (Exception ex)
            {
                return Fail<object>("Database error: " + ex.Message, "DB_ERROR");
            }
        }

        // ----------------------------------------------------------------
        //  Resolve identifier (number or numeric ID)
        // ----------------------------------------------------------------
        public SWROperationResult<object> ResolveRegistration(string identifier)
        {
            int id;
            if (int.TryParse(identifier, out id))
            {
                var r = GetRegistration(id, null);
                return r;
            }
            else
            {
                return GetRegistration(null, identifier);
            }
        }

        // ----------------------------------------------------------------
        //  Helpers
        // ----------------------------------------------------------------
        private static void AddNullable(OracleCommand cmd, string name, OracleDbType type, object value)
        {
            var p = cmd.Parameters.Add(name, type);
            p.Value = value ?? (object)DBNull.Value;
        }

        private static void AddOut(OracleCommand cmd, string name, OracleDbType type, int size = 200)
        {
            var p = cmd.Parameters.Add(name, type, size);
            p.Direction = ParameterDirection.Output;
        }

        private static void AddOutCursor(OracleCommand cmd, string name)
        {
            var p = cmd.Parameters.Add(name, OracleDbType.RefCursor);
            p.Direction = ParameterDirection.Output;
        }

        private static string GetOutString(OracleCommand cmd, string name)
        {
            var val = ((OracleParameter)cmd.Parameters[name]).Value;
            if (val == null || val is DBNull) return null;
            return val.ToString();
        }

        private static int GetOutInt(OracleCommand cmd, string name)
        {
            var val = ((OracleParameter)cmd.Parameters[name]).Value;
            if (val == null || val is DBNull) return 0;
            if (val is OracleDecimal od) return (int)od.Value;
            int result;
            return int.TryParse(val.ToString(), out result) ? result : 0;
        }

        private static DateTime? ParseDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            DateTime dt;
            return DateTime.TryParse(value, out dt) ? dt : (DateTime?)null;
        }

        private static decimal? ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            decimal d;
            return decimal.TryParse(value, out d) ? d : (decimal?)null;
        }

        private static SWROperationResult<T> Fail<T>(string message, string errorCode)
        {
            return new SWROperationResult<T> { Success = false, Message = message, ErrorCode = errorCode };
        }

        private static Dictionary<string, object> ReadSingleRow(OracleRefCursor cursor)
        {
            if (cursor == null) return null;
            using (var reader = cursor.GetDataReader())
            {
                if (!reader.Read()) return null;
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                    row[reader.GetName(i).ToLowerInvariant()] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                return row;
            }
        }

        private static List<Dictionary<string, object>> ReadRows(OracleRefCursor cursor)
        {
            var list = new List<Dictionary<string, object>>();
            if (cursor == null) return list;
            using (var reader = cursor.GetDataReader())
            {
                while (reader.Read())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                        row[reader.GetName(i).ToLowerInvariant()] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    list.Add(row);
                }
            }
            return list;
        }
    }
}
