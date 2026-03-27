using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using SmkcApi.Models.BoothMapping;

namespace SmkcApi.Repositories.BoothMapping
{
    /// <summary>
    /// Repository for booth mapping authentication using WEBSITE schema
    /// </summary>
    public class BoothAuthRepository : IBoothAuthRepository
    {
        private readonly string _connectionString;

        public BoothAuthRepository()
        {
            // Use WEBSITE schema connection (website/website) for authentication
            // SP_USER_LOGIN is in the WEBSITE schema
            _connectionString = ConfigurationManager.ConnectionStrings["OracleDbWebsite"].ConnectionString;
        }

        /// <summary>
        /// Authenticates user using SP_USER_LOGIN stored procedure in WEBSITE schema
        /// </summary>
        public BoothApiResponse<BoothLoginResponse> Login(string userId, string password)
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    using (OracleCommand cmd = new OracleCommand("SP_USER_LOGIN", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Input parameters
                        cmd.Parameters.Add("P_USER_ID", OracleDbType.Varchar2).Value = userId;
                        cmd.Parameters.Add("P_PASSWORD", OracleDbType.Varchar2).Value = password;

                        // Output parameters
                        cmd.Parameters.Add("P_SUCCESS", OracleDbType.Int32).Direction = ParameterDirection.Output;
                        cmd.Parameters.Add("P_MESSAGE", OracleDbType.NVarchar2, 500).Direction = ParameterDirection.Output;
                        cmd.Parameters.Add("P_USER_DATA", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                        conn.Open();

                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            int success = Convert.ToInt32(((OracleDecimal)cmd.Parameters["P_SUCCESS"].Value).ToInt32());
                            string message = cmd.Parameters["P_MESSAGE"].Value?.ToString() ?? "Unknown error";

                            if (success == 1 && reader.Read())
                            {
                                var loginData = new BoothLoginResponse
                                {
                                    UserId = reader["userId"]?.ToString(),
                                    UserName = reader["userName"]?.ToString(),
                                    Role = reader["role"]?.ToString(),
                                    Token = reader["token"]?.ToString()
                                };

                                return BoothApiResponse<BoothLoginResponse>.CreateSuccess(loginData, message);
                            }
                            else
                            {
                                return BoothApiResponse<BoothLoginResponse>.CreateError(message);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"BoothAuthRepository.Login error: {ex.Message}");
                return BoothApiResponse<BoothLoginResponse>.CreateError($"Error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Repository for booth data operations using WEBSITE schema
    /// </summary>
    public class BoothRepository : IBoothRepository
    {
        private readonly string _connectionString;

        public BoothRepository()
        {
            // Use WEBSITE schema connection (website/website) for booth operations
            // All booth stored procedures are in the WEBSITE schema
            _connectionString = ConfigurationManager.ConnectionStrings["OracleDbWebsite"].ConnectionString;
        }

        /// <summary>
        /// Gets booth statistics using SP_GET_STATISTICS
        /// </summary>
        public BoothApiResponse<BoothStatisticsResponse> GetStatistics(string userId = null)
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    using (OracleCommand cmd = new OracleCommand("SP_GET_STATISTICS", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Input parameters
                        cmd.Parameters.Add("P_USER_ID", OracleDbType.Varchar2).Value = 
                            (object)userId ?? DBNull.Value;

                        // Output parameters
                        cmd.Parameters.Add("P_SUCCESS", OracleDbType.Int32).Direction = ParameterDirection.Output;
                        cmd.Parameters.Add("P_MESSAGE", OracleDbType.NVarchar2, 500).Direction = ParameterDirection.Output;
                        cmd.Parameters.Add("P_STATS", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                        conn.Open();

                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            int success = Convert.ToInt32(((OracleDecimal)cmd.Parameters["P_SUCCESS"].Value).ToInt32());
                            string message = cmd.Parameters["P_MESSAGE"].Value?.ToString() ?? "Unknown error";

                            if (success == 1 && reader.Read())
                            {
                                var stats = new BoothStatisticsResponse
                                {
                                    TotalBooths = reader["totalBooths"] != DBNull.Value 
                                        ? Convert.ToInt32(reader["totalBooths"]) : 0,
                                    MappedBooths = reader["mappedBooths"] != DBNull.Value 
                                        ? Convert.ToInt32(reader["mappedBooths"]) : 0,
                                    UnmappedBooths = reader["unmappedBooths"] != DBNull.Value 
                                        ? Convert.ToInt32(reader["unmappedBooths"]) : 0,
                                    UserMappedBooths = reader["userMappedBooths"] != DBNull.Value 
                                        ? Convert.ToInt32(reader["userMappedBooths"]) : 0,
                                    UserUnmappedBooths = reader["userUnmappedBooths"] != DBNull.Value 
                                        ? Convert.ToInt32(reader["userUnmappedBooths"]) : 0
                                };

                                return BoothApiResponse<BoothStatisticsResponse>.CreateSuccess(stats, message);
                            }
                            else
                            {
                                return BoothApiResponse<BoothStatisticsResponse>.CreateError(message);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"BoothRepository.GetStatistics error: {ex.Message}");
                return BoothApiResponse<BoothStatisticsResponse>.CreateError($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets all booths using SP_GET_ALL_BOOTHS
        /// </summary>
        public BoothApiResponse<List<BoothResponse>> GetAllBooths()
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    using (OracleCommand cmd = new OracleCommand("SP_GET_ALL_BOOTHS", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Output parameters
                        cmd.Parameters.Add("P_SUCCESS", OracleDbType.Int32).Direction = ParameterDirection.Output;
                        cmd.Parameters.Add("P_MESSAGE", OracleDbType.NVarchar2, 500).Direction = ParameterDirection.Output;
                        cmd.Parameters.Add("P_BOOTHS", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                        conn.Open();

                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            int success = Convert.ToInt32(((OracleDecimal)cmd.Parameters["P_SUCCESS"].Value).ToInt32());
                            string message = cmd.Parameters["P_MESSAGE"].Value?.ToString() ?? "Unknown error";

                            if (success == 1)
                            {
                                var booths = new List<BoothResponse>();
                                while (reader.Read())
                                {
                                    booths.Add(MapBoothFromReader(reader));
                                }

                                return BoothApiResponse<List<BoothResponse>>.CreateSuccess(booths, message);
                            }
                            else
                            {
                                return BoothApiResponse<List<BoothResponse>>.CreateError(message);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"BoothRepository.GetAllBooths error: {ex.Message}");
                return BoothApiResponse<List<BoothResponse>>.CreateError($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Searches booths with filters using SP_SEARCH_BOOTHS
        /// </summary>
        public BoothApiResponse<List<BoothResponse>> SearchBooths(
            string boothNo, 
            string boothName, 
            string boothAddress, 
            string wardNo, 
            int? isMapped)
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    using (OracleCommand cmd = new OracleCommand("SP_SEARCH_BOOTHS", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Input parameters (use DBNull.Value for nulls)
                        cmd.Parameters.Add("P_BOOTH_NO", OracleDbType.Varchar2).Value = 
                            (object)boothNo ?? DBNull.Value;
                        cmd.Parameters.Add("P_BOOTH_NAME", OracleDbType.NVarchar2).Value = 
                            (object)boothName ?? DBNull.Value;
                        cmd.Parameters.Add("P_BOOTH_ADDRESS", OracleDbType.NVarchar2).Value = 
                            (object)boothAddress ?? DBNull.Value;
                        cmd.Parameters.Add("P_WARD_NO", OracleDbType.Varchar2).Value = 
                            (object)wardNo ?? DBNull.Value;
                        cmd.Parameters.Add("P_IS_MAPPED", OracleDbType.Int32).Value = 
                            isMapped.HasValue ? (object)isMapped.Value : DBNull.Value;

                        // Output parameters
                        cmd.Parameters.Add("P_SUCCESS", OracleDbType.Int32).Direction = ParameterDirection.Output;
                        cmd.Parameters.Add("P_MESSAGE", OracleDbType.NVarchar2, 500).Direction = ParameterDirection.Output;
                        cmd.Parameters.Add("P_BOOTHS", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                        conn.Open();

                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            int success = Convert.ToInt32(((OracleDecimal)cmd.Parameters["P_SUCCESS"].Value).ToInt32());
                            string message = cmd.Parameters["P_MESSAGE"].Value?.ToString() ?? "Unknown error";

                            if (success == 1)
                            {
                                var booths = new List<BoothResponse>();
                                while (reader.Read())
                                {
                                    booths.Add(MapBoothFromReader(reader));
                                }

                                return BoothApiResponse<List<BoothResponse>>.CreateSuccess(booths, message);
                            }
                            else
                            {
                                return BoothApiResponse<List<BoothResponse>>.CreateError(message);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"BoothRepository.SearchBooths error: {ex.Message}");
                return BoothApiResponse<List<BoothResponse>>.CreateError($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates booth GPS location using SP_UPDATE_BOOTH_LOCATION
        /// </summary>
        public BoothApiResponse<BoothResponse> UpdateBoothLocation(
            string boothId, 
            decimal latitude, 
            decimal longitude, 
            string userId, 
            string remarks)
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    using (OracleCommand cmd = new OracleCommand("SP_UPDATE_BOOTH_LOCATION", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Input parameters
                        cmd.Parameters.Add("P_BOOTH_ID", OracleDbType.Varchar2).Value = boothId;
                        cmd.Parameters.Add("P_LATITUDE", OracleDbType.Decimal).Value = latitude;
                        cmd.Parameters.Add("P_LONGITUDE", OracleDbType.Decimal).Value = longitude;
                        cmd.Parameters.Add("P_USER_ID", OracleDbType.Varchar2).Value = userId;
                        cmd.Parameters.Add("P_REMARKS", OracleDbType.NVarchar2).Value = 
                            (object)remarks ?? DBNull.Value;

                        // Output parameters
                        cmd.Parameters.Add("P_SUCCESS", OracleDbType.Int32).Direction = ParameterDirection.Output;
                        cmd.Parameters.Add("P_MESSAGE", OracleDbType.NVarchar2, 500).Direction = ParameterDirection.Output;
                        cmd.Parameters.Add("P_BOOTH_DATA", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                        conn.Open();

                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            int success = Convert.ToInt32(((OracleDecimal)cmd.Parameters["P_SUCCESS"].Value).ToInt32());
                            string message = cmd.Parameters["P_MESSAGE"].Value?.ToString() ?? "Unknown error";

                            if (success == 1 && reader.Read())
                            {
                                var booth = MapBoothFromReader(reader);
                                return BoothApiResponse<BoothResponse>.CreateSuccess(booth, message);
                            }
                            else
                            {
                                return BoothApiResponse<BoothResponse>.CreateError(message);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"BoothRepository.UpdateBoothLocation error: {ex.Message}");
                return BoothApiResponse<BoothResponse>.CreateError($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper method to map Oracle data reader to BoothResponse
        /// </summary>
        private BoothResponse MapBoothFromReader(OracleDataReader reader)
        {
            return new BoothResponse
            {
                Id = reader["id"]?.ToString(),
                BoothNo = reader["boothNo"]?.ToString(),
                BoothName = reader["boothName"]?.ToString(),
                BoothNameEnglish = reader["boothNameEnglish"]?.ToString(),
                BoothAddress = reader["boothAddress"]?.ToString(),
                BoothAddressEnglish = reader["boothAddressEnglish"]?.ToString(),
                WardNo = reader["wardNo"]?.ToString(),
                WardName = reader["wardName"]?.ToString(),
                Latitude = reader["latitude"] != DBNull.Value 
                    ? Convert.ToDecimal(reader["latitude"]) 
                    : (decimal?)null,
                Longitude = reader["longitude"] != DBNull.Value 
                    ? Convert.ToDecimal(reader["longitude"]) 
                    : (decimal?)null,
                IsMapped = reader["isMapped"]?.ToString(),
                MappedBy = reader["mappedBy"] != DBNull.Value 
                    ? reader["mappedBy"].ToString() 
                    : null,
                MappedDate = reader["mappedDate"] != DBNull.Value 
                    ? reader["mappedDate"].ToString() 
                    : null
            };
        }
    }
}
