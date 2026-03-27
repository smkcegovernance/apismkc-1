using System;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using SmkcApi.Models.DepositManager;
using SmkcApi.Models.VotingStatistics;

namespace SmkcApi.Repositories.VotingStatistics
{
    public interface IVotingStatisticsRepository
    {
        ApiResponse GetVotingStatistics();
        ApiResponse GetLatestStatistics();
        ApiResponse UpdateVotingStatistics(UpdateVotingStatisticsRequest request);
    }

    public class VotingStatisticsRepository : IVotingStatisticsRepository
    {
        private readonly IOracleConnectionFactory _connFactory;

        public VotingStatisticsRepository(IOracleConnectionFactory connFactory)
        {
            _connFactory = connFactory ?? throw new ArgumentNullException(nameof(connFactory));
        }

        /// <summary>
        /// Get the latest active voting statistics record
        /// </summary>
        public ApiResponse GetVotingStatistics()
        {
            return ExecuteWebsite("SP_GET_VOTING_STATISTICS", cmd =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("o_success", OracleDbType.Int32).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_message", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_statistics", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
            });
        }

        /// <summary>
        /// Get formatted statistics from the view with calculated percentages
        /// </summary>
        public ApiResponse GetLatestStatistics()
        {
            return ExecuteWebsite("SP_GET_VOTING_STATISTICS", cmd =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("o_success", OracleDbType.Int32).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_message", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_statistics", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
            });
        }

        /// <summary>
        /// Update or insert voting statistics
        /// </summary>
        public ApiResponse UpdateVotingStatistics(UpdateVotingStatisticsRequest request)
        {
            return ExecuteWebsite("SP_UPDATE_VOTING_STATISTICS", cmd =>
            {
                cmd.CommandType = CommandType.StoredProcedure;

                // Input parameters
                cmd.Parameters.Add("p_total_voters", OracleDbType.Int32).Value = request.TotalVoters;
                cmd.Parameters.Add("p_male_voters", OracleDbType.Int32).Value = request.MaleVoters;
                cmd.Parameters.Add("p_female_voters", OracleDbType.Int32).Value = request.FemaleVoters;
                cmd.Parameters.Add("p_other_voters", OracleDbType.Int32).Value = request.OtherVoters;
                cmd.Parameters.Add("p_casted_votes", OracleDbType.Int32).Value = request.CastedVotes;
                cmd.Parameters.Add("p_male_casted", OracleDbType.Int32).Value = request.MaleCasted;
                cmd.Parameters.Add("p_female_casted", OracleDbType.Int32).Value = request.FemaleCasted;
                cmd.Parameters.Add("p_other_casted", OracleDbType.Int32).Value = request.OtherCasted;
                cmd.Parameters.Add("p_time_slot", OracleDbType.Varchar2).Value = request.TimeSlot;
                cmd.Parameters.Add("p_updated_by", OracleDbType.Varchar2).Value = request.UpdatedBy;

                // Output parameters
                cmd.Parameters.Add("o_result", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
            });
        }

        private ApiResponse ExecuteWebsite(string spName, Action<OracleCommand> fill)
        {
            try
            {
                using (var conn = _connFactory.CreateWebsite())
                using (var cmd = new OracleCommand(spName, conn))
                {
                    fill(cmd);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    // Check if this is an update operation (has o_result instead of o_success)
                    if (cmd.Parameters.Contains("o_result"))
                    {
                        var result = Convert.ToString(cmd.Parameters["o_result"].Value);
                        var updateSuccess = result != null && result.StartsWith("SUCCESS", StringComparison.OrdinalIgnoreCase);

                        var updateData = new UpdateStatisticsResult
                        {
                            Result = result ?? "ERROR: No result returned",
                            Timestamp = DateTime.UtcNow
                        };

                        return new ApiResponse
                        {
                            Success = updateSuccess,
                            Message = updateSuccess ? "Voting statistics updated successfully" : "Failed to update voting statistics",
                            Data = updateData
                        };
                    }

                    // Standard response with o_success and o_message
                    var oSuccessVal = cmd.Parameters["o_success"].Value;
                    int successCode;

                    if (oSuccessVal is OracleDecimal oracleDecimal)
                    {
                        successCode = oracleDecimal.ToInt32();
                    }
                    else
                    {
                        successCode = Convert.ToInt32(oSuccessVal);
                    }

                    var success = successCode == 1;
                    var message = Convert.ToString(cmd.Parameters["o_message"].Value);

                    var data = OracleRefCursorReader.ReadAll(cmd);

                    return new ApiResponse
                    {
                        Success = success,
                        Message = message,
                        Data = data
                    };
                }
            }
            catch (OracleException ex)
            {
                System.Diagnostics.Trace.TraceError($"Oracle Error in {spName}: {ex.Message}\nStack: {ex.StackTrace}");
                return new ApiResponse
                {
                    Success = false,
                    Message = "Database error",
                    Error = ex.Message
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"Error in {spName}: {ex.Message}\nStack: {ex.StackTrace}");
                return new ApiResponse
                {
                    Success = false,
                    Message = "Internal server error",
                    Error = ex.Message
                };
            }
        }

        private static class OracleRefCursorReader
        {
            public static object ReadAll(OracleCommand cmd)
            {
                foreach (OracleParameter p in cmd.Parameters)
                {
                    if (p.OracleDbType == OracleDbType.RefCursor)
                    {
                        if (p.Value != null && p.Value != DBNull.Value && p.Value is OracleRefCursor)
                        {
                            var refCursor = (OracleRefCursor)p.Value;
                            try
                            {
                                using (var reader = refCursor.GetDataReader())
                                {
                                    var table = new DataTable();
                                    table.Load(reader);
                                    return table;
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Trace.TraceError("Error reading RefCursor " + p.ParameterName + ": " + ex.Message);
                                return new DataTable();
                            }
                        }
                        else
                        {
                            return new DataTable();
                        }
                    }
                }

                return null;
            }
        }
    }
}
