using System;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using SmkcApi.Models.DepositManager;
using SmkcApi.Repositories;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace SmkcApi.Repositories.DepositManager
{
    public interface IDepositRepository
    {
        ApiResponse Bank_GetRequirements(string status, string depositType);
        ApiResponse Bank_GetQuotes(string bankId, string requirementId);
        ApiResponse Bank_SubmitQuote(SubmitQuoteRequest request);
        ApiResponse Bank_GetDashboardStats(string bankId);
        ApiResponse Account_GetRequirements(string status, string depositType);
        ApiResponse Account_CreateRequirement(CreateRequirementRequest request);
        ApiResponse Account_PublishRequirement(string requirementId, string authorizedBy);
        ApiResponse Account_DeleteRequirement(string requirementId);
        ApiResponse Account_GetBanks(string status);
        ApiResponse Account_CreateBank(CreateBankRequest request);
        ApiResponse Account_GetQuotes(string requirementId);
        ApiResponse Account_GetDashboardStats();
        ApiResponse Commissioner_GetRequirements(string status);
        ApiResponse Commissioner_GetRequirementWithQuotes(string requirementId);
        ApiResponse Commissioner_GetQuotes(string requirementId);
        ApiResponse Commissioner_AuthorizeRequirement(string requirementId, string commissionerId);
        ApiResponse Commissioner_FinalizeDeposit(string requirementId, string bankId);
        ApiResponse Commissioner_GetDashboardStats();
        ApiResponse Commissioner_GetEnhancedKpis(string bankId, string depositType, DateTime? fromDate, DateTime? toDate);
        ApiResponse Commissioner_GetBankWiseAnalytics(string depositType, DateTime? fromDate, DateTime? toDate);
        ApiResponse Commissioner_GetUpcomingMaturities(int? withinDays, string bankId, string depositType, int? minDaysLeft, int? maxDaysLeft, string schemeName);
        ApiResponse Commissioner_GetDepositTypeDistribution(string bankId, DateTime? fromDate, DateTime? toDate);
        ApiResponse Commissioner_GetInterestTimeline(int? monthsAhead, string bankId, string depositType, DateTime? fromDate, DateTime? toDate);
        ApiResponse Commissioner_GetPortfolioHealth(int? minDiversifiedBanks, decimal? maxSingleBankPercent, decimal? targetAvgRate, string bankId, string depositType, DateTime? fromDate, DateTime? toDate);
        ApiResponse Common_GetRequirementById(string requirementId);
    }

    public class DepositRepository : IDepositRepository
    {
        private readonly IOracleConnectionFactory _connFactory;
        public DepositRepository(IOracleConnectionFactory connFactory) { _connFactory = connFactory; }

        public ApiResponse Bank_GetRequirements(string status, string depositType)
            => ExecuteAbas("SP_BANK_GET_REQUIREMENTS", cmd =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("p_status", OracleDbType.Varchar2).Value = (object)status ?? DBNull.Value;
                cmd.Parameters.Add("p_deposit_type", OracleDbType.Varchar2).Value = (object)depositType ?? DBNull.Value;
                cmd.Parameters.Add("o_success", OracleDbType.Int32).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_message", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_requirements", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
            });

        public ApiResponse Bank_GetQuotes(string bankId, string requirementId)
            => ExecuteAbas("SP_BANK_GET_QUOTES", cmd =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("p_bank_id", OracleDbType.Varchar2).Value = bankId;
                cmd.Parameters.Add("p_requirement_id", OracleDbType.Varchar2).Value = (object)requirementId ?? DBNull.Value;
                cmd.Parameters.Add("o_success", OracleDbType.Int32).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_message", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_quotes", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
            });

        public ApiResponse Bank_SubmitQuote(SubmitQuoteRequest request)
            => ExecuteAbas("SP_BANK_SUBMIT_QUOTE", cmd =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("p_requirement_id", OracleDbType.Varchar2).Value = request.RequirementId;
                cmd.Parameters.Add("p_bank_id", OracleDbType.Varchar2).Value = request.BankId;
                cmd.Parameters.Add("p_interest_rate", OracleDbType.Decimal).Value = request.InterestRate;
                cmd.Parameters.Add("p_remarks", OracleDbType.Clob).Value = (object)request.Remarks ?? DBNull.Value;
                // New schema: only pass file name, not content or size
                cmd.Parameters.Add("p_consent_file_name", OracleDbType.Varchar2).Value = request.ConsentDocument != null ? request.ConsentDocument.FileName : (object)DBNull.Value;
                cmd.Parameters.Add("o_success", OracleDbType.Int32).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_message", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_quote_data", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
            });

        public ApiResponse Bank_GetDashboardStats(string bankId)
            => ExecuteAbas("SP_BANK_GET_DASHBOARD_STATS", cmd =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("p_bank_id", OracleDbType.Varchar2).Value = bankId;
                cmd.Parameters.Add("o_success", OracleDbType.Int32).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_message", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_stats", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
            });

        public ApiResponse Account_GetRequirements(string status, string depositType)
            => ExecuteAbas("SP_ACCOUNT_GET_REQUIREMENTS", cmd =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("p_status", OracleDbType.Varchar2).Value = (object)status ?? DBNull.Value;
                cmd.Parameters.Add("p_deposit_type", OracleDbType.Varchar2).Value = (object)depositType ?? DBNull.Value;
                cmd.Parameters.Add("p_from_date", OracleDbType.TimeStamp).Value = DBNull.Value;
                cmd.Parameters.Add("p_to_date", OracleDbType.TimeStamp).Value = DBNull.Value;
                cmd.Parameters.Add("o_success", OracleDbType.Int32).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_message", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_requirements", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
            });

        public ApiResponse Account_CreateRequirement(CreateRequirementRequest request)
            => ExecuteAbas("SP_ACCOUNT_CREATE_REQUIREMENT", cmd =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("p_scheme_name", OracleDbType.Varchar2).Value = request.SchemeName;
                cmd.Parameters.Add("p_deposit_type", OracleDbType.Varchar2).Value = request.DepositType;
                cmd.Parameters.Add("p_amount", OracleDbType.Decimal).Value = request.Amount;
                cmd.Parameters.Add("p_deposit_period", OracleDbType.Int32).Value = request.DepositPeriod;
                cmd.Parameters.Add("p_validity_period", OracleDbType.TimeStamp).Value = request.ValidityPeriodUtc;
                cmd.Parameters.Add("p_description", OracleDbType.Clob).Value = (object)request.Description ?? DBNull.Value;
                cmd.Parameters.Add("p_created_by", OracleDbType.Varchar2).Value = request.CreatedBy;
                cmd.Parameters.Add("o_success", OracleDbType.Int32).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_message", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_requirement_data", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
            });

        /// <summary>
        /// Publish requirement using ABAS.PKG_DEPOSIT_REQUIREMENTS.publish_requirement
        /// This procedure does not expose o_success/o_message, so we execute it directly
        /// and map Oracle exceptions to ApiResponse.
        /// </summary>
        public ApiResponse Account_PublishRequirement(string requirementId, string authorizedBy)
        {
            if (string.IsNullOrWhiteSpace(requirementId))
                return new ApiResponse { Success = false, Message = "Requirement ID is required" };

            try
            {
                using (var conn = _connFactory.CreateAbas())
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = "ABAS.PKG_DEPOSIT_REQUIREMENTS.publish_requirement";
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Requirement ID is alphanumeric (e.g., REQ0000000002), so send as Varchar2
                    cmd.Parameters.Add("p_requirement_id", OracleDbType.Varchar2, requirementId, ParameterDirection.Input);
                    cmd.Parameters.Add("p_authorized_by", OracleDbType.Varchar2, authorizedBy, ParameterDirection.Input);

                    cmd.ExecuteNonQuery();

                    return new ApiResponse
                    {
                        Success = true,
                        Message = "Requirement published successfully"
                    };
                }
            }
            catch (OracleException ex)
            {
                var msg = ex.Message ?? string.Empty;
                var lower = msg.ToLowerInvariant();

                if (lower.Contains("not found"))
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Requirement not found",
                        Error = ex.Message
                    };
                }
                if (lower.Contains("not in draft") || lower.Contains("already published") || lower.Contains("invalid status"))
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Requirement is not in draft status and cannot be published",
                        Error = ex.Message
                    };
                }

                System.Diagnostics.Trace.TraceError("Oracle Error in publish_requirement: " + ex.Message + "\n" + ex.StackTrace);
                return new ApiResponse
                {
                    Success = false,
                    Message = "Database error",
                    Error = ex.Message
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Error in publish_requirement: " + ex.Message + "\n" + ex.StackTrace);
                return new ApiResponse
                {
                    Success = false,
                    Message = "Internal server error",
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// Delete requirement using ABAS.PKG_DEPOSIT_REQUIREMENTS.delete_requirement
        /// Now uses p_requirement_id, o_success, o_message as per package signature.
        /// </summary>
        public ApiResponse Account_DeleteRequirement(string requirementId)
        {
            if (string.IsNullOrWhiteSpace(requirementId))
                return new ApiResponse { Success = false, Message = "Requirement ID is required" };

            try
            {
                using (var conn = _connFactory.CreateAbas())
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = "ABAS.PKG_DEPOSIT_REQUIREMENTS.delete_requirement";
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Requirement ID is alphanumeric (e.g., REQ0000000012), send as Varchar2
                    cmd.Parameters.Add("p_requirement_id", OracleDbType.Varchar2, requirementId, ParameterDirection.Input);
                    cmd.Parameters.Add("o_success", OracleDbType.Int32).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("o_message", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;

                    cmd.ExecuteNonQuery();

                    // Map OUT parameters to ApiResponse
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

                    if (!success)
                    {
                        var lower = (message ?? string.Empty).ToLowerInvariant();

                        if (lower.Contains("not found"))
                        {
                            return new ApiResponse
                            {
                                Success = false,
                                Message = "Requirement not found",
                                Error = message
                            };
                        }
                        if (lower.Contains("not in draft") || lower.Contains("cannot be deleted") || lower.Contains("invalid status"))
                        {
                            return new ApiResponse
                            {
                                Success = false,
                                Message = "Requirement is not in draft status and cannot be deleted",
                                Error = message
                            };
                        }

                        return new ApiResponse
                        {
                            Success = false,
                            Message = string.IsNullOrWhiteSpace(message) ? "Delete requirement failed" : message
                        };
                    }

                    return new ApiResponse
                    {
                        Success = true,
                        Message = string.IsNullOrWhiteSpace(message) ? "Requirement deleted successfully" : message
                    };
                }
            }
            catch (OracleException ex)
            {
                System.Diagnostics.Trace.TraceError("Oracle Error in delete_requirement: " + ex.Message + "\n" + ex.StackTrace);
                return new ApiResponse
                {
                    Success = false,
                    Message = "Database error",
                    Error = ex.Message
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Error in delete_requirement: " + ex.Message + "\n" + ex.StackTrace);
                return new ApiResponse
                {
                    Success = false,
                    Message = "Internal server error",
                    Error = ex.Message
                };
            }
        }

        public ApiResponse Account_GetBanks(string status)
            => ExecuteAbas("SP_ACCOUNT_GET_BANKS", cmd =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("p_status", OracleDbType.Varchar2).Value =
                    string.IsNullOrEmpty(status) ? (object)DBNull.Value : status;
                cmd.Parameters.Add("o_success", OracleDbType.Int32).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_message", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_banks", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
            });

        public ApiResponse Account_CreateBank(CreateBankRequest request)
            => ExecuteAbas("SP_ACCOUNT_CREATE_BANK", cmd =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("p_name", OracleDbType.Varchar2).Value = request.Name;
                cmd.Parameters.Add("p_branch_address", OracleDbType.Varchar2).Value = (object)request.BranchAddress ?? DBNull.Value;
                cmd.Parameters.Add("p_address", OracleDbType.Varchar2).Value = (object)request.Address ?? DBNull.Value;
                cmd.Parameters.Add("p_micr", OracleDbType.Varchar2).Value = (object)request.Micr ?? DBNull.Value;
                cmd.Parameters.Add("p_ifsc", OracleDbType.Varchar2).Value = (object)request.Ifsc ?? DBNull.Value;
                cmd.Parameters.Add("p_email", OracleDbType.Varchar2).Value = (object)request.Email ?? DBNull.Value;
                cmd.Parameters.Add("p_contact_person", OracleDbType.Varchar2).Value = (object)request.ContactPerson ?? DBNull.Value;
                cmd.Parameters.Add("p_contact_no", OracleDbType.Varchar2).Value = (object)request.ContactNo ?? DBNull.Value;
                cmd.Parameters.Add("p_phone", OracleDbType.Varchar2).Value = (object)request.Phone ?? DBNull.Value;
                cmd.Parameters.Add("o_success", OracleDbType.Int32).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_message", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_bank_data", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
            });

        public ApiResponse Account_GetQuotes(string requirementId)
            => ExecuteAbas("SP_ACCOUNT_GET_QUOTES", cmd =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("p_requirement_id", OracleDbType.Varchar2).Value = (object)requirementId ?? DBNull.Value;
                cmd.Parameters.Add("p_bank_id", OracleDbType.Varchar2).Value = DBNull.Value;
                cmd.Parameters.Add("o_success", OracleDbType.Int32).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_message", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_quotes", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
            });

        public ApiResponse Account_GetDashboardStats()
            => ExecuteAbas("SP_ACCOUNT_GET_DASHBOARD_STATS", cmd =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("o_success", OracleDbType.Int32).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_message", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_stats", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
            });

        public ApiResponse Commissioner_GetRequirements(string status)
            => ExecuteAbas("SP_COMMISSIONER_GET_REQUIREMENTS", cmd =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("p_status", OracleDbType.Varchar2).Value = (object)status ?? DBNull.Value;
                cmd.Parameters.Add("o_success", OracleDbType.Int32).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_message", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_requirements", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
            });

        public ApiResponse Commissioner_GetRequirementWithQuotes(string requirementId)
            => ExecuteAbas("SP_COMMISSIONER_GET_REQ_DETAILS", cmd =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("p_requirement_id", OracleDbType.Varchar2).Value = requirementId;
                cmd.Parameters.Add("o_success", OracleDbType.Int32).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_message", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_requirement", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_quotes", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
            });

        public ApiResponse Commissioner_GetQuotes(string requirementId)
            => ExecuteAbas("SP_COMMISSIONER_GET_QUOTES", cmd =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("p_requirement_id", OracleDbType.Varchar2).Value = (object)requirementId ?? DBNull.Value;
                cmd.Parameters.Add("o_success", OracleDbType.Int32).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_message", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_quotes", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
            });

        public ApiResponse Commissioner_AuthorizeRequirement(string requirementId, string commissionerId)
            => ExecuteAbas("SP_COMMISSIONER_AUTHORIZE_REQ", cmd =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("p_requirement_id", OracleDbType.Varchar2).Value = requirementId;
                cmd.Parameters.Add("p_commissioner_id", OracleDbType.Varchar2).Value = commissionerId;
                cmd.Parameters.Add("o_success", OracleDbType.Int32).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_message", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_requirement_data", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
            });

        public ApiResponse Commissioner_FinalizeDeposit(string requirementId, string bankId)
            => ExecuteAbas("SP_COMMISSIONER_FINALIZE_DEPOSIT", cmd =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("p_requirement_id", OracleDbType.Varchar2).Value = requirementId;
                cmd.Parameters.Add("p_bank_id", OracleDbType.Varchar2).Value = bankId;
                cmd.Parameters.Add("o_success", OracleDbType.Int32).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_message", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_result_data", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
            });
        
        public ApiResponse Commissioner_GetDashboardStats()
            => ExecuteAbas("SP_COMMISSIONER_GET_DASHBOARD_STATS", cmd =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("o_success", OracleDbType.Int32).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_message", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_stats", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
            });

        public ApiResponse Commissioner_GetEnhancedKpis(string bankId, string depositType, DateTime? fromDate, DateTime? toDate)
            => ExecuteAbas("SP_COMMISSIONER_GET_ENHANCED_KPIS", cmd =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("p_bank_id", OracleDbType.Varchar2).Value = (object)bankId ?? DBNull.Value;
                cmd.Parameters.Add("p_deposit_type", OracleDbType.Varchar2).Value = (object)depositType ?? DBNull.Value;
                cmd.Parameters.Add("p_from_date", OracleDbType.TimeStamp).Value = fromDate.HasValue ? (object)fromDate.Value : DBNull.Value;
                cmd.Parameters.Add("p_to_date", OracleDbType.TimeStamp).Value = toDate.HasValue ? (object)toDate.Value : DBNull.Value;
                cmd.Parameters.Add("o_success", OracleDbType.Int32).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_message", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_kpis", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
            });

        public ApiResponse Commissioner_GetBankWiseAnalytics(string depositType, DateTime? fromDate, DateTime? toDate)
            => ExecuteAbas("SP_COMMISSIONER_GET_BANK_WISE_ANALYTICS", cmd =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("p_deposit_type", OracleDbType.Varchar2).Value = (object)depositType ?? DBNull.Value;
                cmd.Parameters.Add("p_from_date", OracleDbType.TimeStamp).Value = fromDate.HasValue ? (object)fromDate.Value : DBNull.Value;
                cmd.Parameters.Add("p_to_date", OracleDbType.TimeStamp).Value = toDate.HasValue ? (object)toDate.Value : DBNull.Value;
                cmd.Parameters.Add("o_success", OracleDbType.Int32).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_message", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_bank_analytics", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
            });

        public ApiResponse Commissioner_GetUpcomingMaturities(int? withinDays, string bankId, string depositType, int? minDaysLeft, int? maxDaysLeft, string schemeName)
            => ExecuteAbas("SP_COMMISSIONER_GET_UPCOMING_MATURITIES", cmd =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("p_within_days", OracleDbType.Int32).Value = withinDays.HasValue ? (object)withinDays.Value : DBNull.Value;
                cmd.Parameters.Add("p_bank_id", OracleDbType.Varchar2).Value = (object)bankId ?? DBNull.Value;
                cmd.Parameters.Add("p_deposit_type", OracleDbType.Varchar2).Value = (object)depositType ?? DBNull.Value;
                cmd.Parameters.Add("p_min_days_left", OracleDbType.Int32).Value = minDaysLeft.HasValue ? (object)minDaysLeft.Value : DBNull.Value;
                cmd.Parameters.Add("p_max_days_left", OracleDbType.Int32).Value = maxDaysLeft.HasValue ? (object)maxDaysLeft.Value : DBNull.Value;
                cmd.Parameters.Add("p_scheme_name", OracleDbType.Varchar2).Value = (object)schemeName ?? DBNull.Value;
                cmd.Parameters.Add("o_success", OracleDbType.Int32).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_message", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_maturities", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
            });

        public ApiResponse Commissioner_GetDepositTypeDistribution(string bankId, DateTime? fromDate, DateTime? toDate)
            => ExecuteAbas("SP_COMMISSIONER_GET_DEPOSIT_TYPE_DISTRIBUTION", cmd =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("p_bank_id", OracleDbType.Varchar2).Value = (object)bankId ?? DBNull.Value;
                cmd.Parameters.Add("p_from_date", OracleDbType.TimeStamp).Value = fromDate.HasValue ? (object)fromDate.Value : DBNull.Value;
                cmd.Parameters.Add("p_to_date", OracleDbType.TimeStamp).Value = toDate.HasValue ? (object)toDate.Value : DBNull.Value;
                cmd.Parameters.Add("o_success", OracleDbType.Int32).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_message", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_distribution", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
            });

        public ApiResponse Commissioner_GetInterestTimeline(int? monthsAhead, string bankId, string depositType, DateTime? fromDate, DateTime? toDate)
            => ExecuteAbas("SP_COMMISSIONER_GET_INTEREST_TIMELINE", cmd =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("p_months_ahead", OracleDbType.Int32).Value = monthsAhead.HasValue ? (object)monthsAhead.Value : DBNull.Value;
                cmd.Parameters.Add("p_bank_id", OracleDbType.Varchar2).Value = (object)bankId ?? DBNull.Value;
                cmd.Parameters.Add("p_deposit_type", OracleDbType.Varchar2).Value = (object)depositType ?? DBNull.Value;
                cmd.Parameters.Add("p_from_date", OracleDbType.TimeStamp).Value = fromDate.HasValue ? (object)fromDate.Value : DBNull.Value;
                cmd.Parameters.Add("p_to_date", OracleDbType.TimeStamp).Value = toDate.HasValue ? (object)toDate.Value : DBNull.Value;
                cmd.Parameters.Add("o_success", OracleDbType.Int32).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_message", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_timeline", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
            });

        public ApiResponse Commissioner_GetPortfolioHealth(int? minDiversifiedBanks, decimal? maxSingleBankPercent, decimal? targetAvgRate, string bankId, string depositType, DateTime? fromDate, DateTime? toDate)
            => ExecuteAbas("SP_COMMISSIONER_GET_PORTFOLIO_HEALTH", cmd =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("p_min_diversified_banks", OracleDbType.Int32).Value = minDiversifiedBanks.HasValue ? (object)minDiversifiedBanks.Value : DBNull.Value;
                cmd.Parameters.Add("p_max_single_bank_percent", OracleDbType.Decimal).Value = maxSingleBankPercent.HasValue ? (object)maxSingleBankPercent.Value : DBNull.Value;
                cmd.Parameters.Add("p_target_avg_rate", OracleDbType.Decimal).Value = targetAvgRate.HasValue ? (object)targetAvgRate.Value : DBNull.Value;
                cmd.Parameters.Add("p_bank_id", OracleDbType.Varchar2).Value = (object)bankId ?? DBNull.Value;
                cmd.Parameters.Add("p_deposit_type", OracleDbType.Varchar2).Value = (object)depositType ?? DBNull.Value;
                cmd.Parameters.Add("p_from_date", OracleDbType.TimeStamp).Value = fromDate.HasValue ? (object)fromDate.Value : DBNull.Value;
                cmd.Parameters.Add("p_to_date", OracleDbType.TimeStamp).Value = toDate.HasValue ? (object)toDate.Value : DBNull.Value;
                cmd.Parameters.Add("o_success", OracleDbType.Int32).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_message", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_health", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
            });

        public ApiResponse Common_GetRequirementById(string requirementId)
            => ExecuteAbas("SP_COMMISSIONER_GET_REQ_DETAILS", cmd =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("p_requirement_id", OracleDbType.Varchar2).Value = requirementId;
                cmd.Parameters.Add("o_success", OracleDbType.Int32).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_message", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_requirement", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("o_quotes", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
            });

        private ApiResponse ExecuteAbas(string spName, Action<OracleCommand> fill)
        {
            try
            {
                using (var conn = _connFactory.CreateAbas())
                using (var cmd = new OracleCommand(spName, conn))
                {
                    fill(cmd);
                    
                    // SERIALIZE INPUT PARAMETERS TO JSON AND LOG TO CONSOLE
                    var inputParams = new Dictionary<string, object>();
                    foreach (OracleParameter p in cmd.Parameters)
                    {
                        if (p.Direction == ParameterDirection.Input || p.Direction == ParameterDirection.InputOutput)
                        {
                            object paramValue;
                            if (p.Value == null || p.Value == DBNull.Value)
                            {
                                paramValue = null;
                            }
                            else if (p.OracleDbType == OracleDbType.Clob)
                            {
                                paramValue = p.Value.ToString();
                            }
                            else if (p.Value is OracleDecimal)
                            {
                                var oracleDecimalValue = (OracleDecimal)p.Value;
                                paramValue = oracleDecimalValue.IsNull ? null : (object)oracleDecimalValue.Value;
                            }
                            else
                            {
                                paramValue = p.Value;
                            }
                            inputParams[p.ParameterName] = paramValue;
                        }
                    }
                    
                    var inputParamsJson = JsonConvert.SerializeObject(inputParams, Formatting.Indented);
                    Console.WriteLine("=== INPUT PARAMETERS JSON ===");
                    Console.WriteLine($"Stored Procedure: {spName}");
                    Console.WriteLine(inputParamsJson);
                    Console.WriteLine("=============================");
                    
                    // Also log to trace
                    System.Diagnostics.Trace.TraceInformation($"=== INPUT PARAMETERS JSON ===\nStored Procedure: {spName}\n{inputParamsJson}\n=============================");
                    
                    conn.Open();
                    cmd.ExecuteNonQuery();

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
                    
                    // ENHANCED LOGGING FOR DEBUGGING
                    var debugInfo = new System.Text.StringBuilder();
                    debugInfo.AppendLine($"=== STORED PROCEDURE DEBUG: {spName} ===");
                    debugInfo.AppendLine($"Success: {success}");
                    debugInfo.AppendLine($"Message: {message}");
                    debugInfo.AppendLine("Parameters:");
                    foreach (OracleParameter p in cmd.Parameters)
                    {
                        if (p.OracleDbType == OracleDbType.RefCursor)
                        {
                            debugInfo.AppendLine($"  - {p.ParameterName} (RefCursor): {(p.Value != null && p.Value != DBNull.Value ? "HasValue" : "NULL")}");
                        }
                        else if (p.Direction == ParameterDirection.Output || p.Direction == ParameterDirection.InputOutput)
                        {
                            debugInfo.AppendLine($"  - {p.ParameterName}: {p.Value ?? "NULL"}");
                        }
                    }
                    
                    var data = OracleRefCursorReader.ReadAll(cmd);
                    
                    debugInfo.AppendLine($"Data Type: {data?.GetType().Name ?? "NULL"}");
                    if (data is System.Collections.Generic.Dictionary<string, object> dict)
                    {
                        debugInfo.AppendLine($"Dictionary Keys: {string.Join(", ", dict.Keys)}");
                        foreach (var kv in dict)
                        {
                            if (kv.Value is DataTable dt)
                            {
                                debugInfo.AppendLine($"  - {kv.Key}: DataTable with {dt.Rows.Count} rows, {dt.Columns.Count} columns");
                            }
                            else
                            {
                                debugInfo.AppendLine($"  - {kv.Key}: {kv.Value?.GetType().Name ?? "NULL"}");
                            }
                        }
                    }
                    else if (data is DataTable singleDt)
                    {
                        debugInfo.AppendLine($"Single DataTable: {singleDt.Rows.Count} rows, {singleDt.Columns.Count} columns");
                    }
                    debugInfo.AppendLine("=====================================");
                    
                    System.Diagnostics.Trace.TraceInformation(debugInfo.ToString());

                    return new ApiResponse { Success = success, Message = message, Data = data };
                }
            }
            catch (OracleException ex)
            {
                System.Diagnostics.Trace.TraceError($"Oracle Error in {spName}: {ex.Message}\nStack: {ex.StackTrace}");
                return new ApiResponse { Success = false, Message = "Database error", Error = ex.Message };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"Error in {spName}: {ex.Message}\nStack: {ex.StackTrace}");
                return new ApiResponse { Success = false, Message = "Internal server error", Error = ex.Message };
            }
        }
    }

    internal static class OracleRefCursorReader
    {
        public static object ReadAll(OracleCommand cmd)
        {
            var result = new System.Collections.Generic.Dictionary<string, object>();
            var refCursorCount = 0;
            
            // First pass: count RefCursors and read them
            foreach (OracleParameter p in cmd.Parameters)
            {
                if (p.OracleDbType == OracleDbType.RefCursor)
                {
                    refCursorCount++;
                    
                    if (p.Value != null && p.Value != DBNull.Value && p.Value is OracleRefCursor)
                    {
                        var refCursor = (OracleRefCursor)p.Value;
                        try
                        {
                            using (var reader = refCursor.GetDataReader())
                            {
                                var table = new DataTable();
                                table.Load(reader);
                                result[p.ParameterName] = table;
                                
                                System.Diagnostics.Trace.TraceInformation(
                                    string.Format("RefCursor '{0}' loaded: {1} rows, {2} columns", p.ParameterName, table.Rows.Count, table.Columns.Count));
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Trace.TraceError("Error reading RefCursor " + p.ParameterName + ": " + ex.Message);
                            result[p.ParameterName] = new DataTable(); // Empty table on error
                        }
                    }
                    else
                    {
                        System.Diagnostics.Trace.TraceWarning("RefCursor '" + p.ParameterName + "' is NULL or invalid");
                        result[p.ParameterName] = new DataTable(); // Empty table if NULL
                    }
                }
            }
            
            // Return logic based on count
            if (refCursorCount == 1)
            {
                // Single RefCursor: return DataTable directly for backward compatibility
                foreach (var kv in result)
                {
                    return kv.Value;
                }
            }
            
            // Multiple RefCursors: return dictionary
            return result;
        }
    }
}
