using SmkcApi.Models.DepositManager;
using SmkcApi.Repositories.DepositManager;

namespace SmkcApi.Services.DepositManager
{
    public class BankService : IBankService
    {
        private readonly IDepositRepository _repo;
        private readonly IFtpStorageService _ftpStorage;

        public BankService(IDepositRepository repo, IFtpStorageService ftpStorage)
        {
            _repo = repo;
            _ftpStorage = ftpStorage;
        }

        public ApiResponse GetRequirements(string status, string depositType) => _repo.Bank_GetRequirements(status, depositType);
        public ApiResponse GetRequirementById(string id) => _repo.Common_GetRequirementById(id);
        public ApiResponse GetQuotes(string bankId, string requirementId) => _repo.Bank_GetQuotes(bankId, requirementId);
        public ApiResponse GetDashboardStats(string bankId) => _repo.Bank_GetDashboardStats(bankId);

        public ApiResponse SubmitQuote(SubmitQuoteRequest request)
        {
            // Validate that consent document is provided
            if (request == null || request.ConsentDocument == null)
            {
                return new ApiResponse
                {
                    Success = false,
                    Message = "Consent document is required"
                };
            }

            string consentFileName = null;
            var hasInlineFileData = !string.IsNullOrWhiteSpace(request.ConsentDocument.FileData);

            // If inline base64 is provided, upload now and persist generated storage file name.
            // If only fileName is provided, treat it as already uploaded by the dedicated upload API.
            if (hasInlineFileData)
            {
                try
                {
                    // UploadConsentDocument now returns only the file name (e.g., "{guid}_{originalName}.pdf")
                    consentFileName = _ftpStorage.UploadConsentDocument(
                        request.RequirementId,
                        request.BankId,
                        request.ConsentDocument);

                    System.Diagnostics.Trace.TraceInformation(
                        string.Format("Consent document uploaded to FTP - Requirement={0}, Bank={1}, FileName={2}",
                            request.RequirementId,
                            request.BankId,
                            consentFileName));

                    // Update the FileName in request to be the unique storage file name for DB persistence
                    request.ConsentDocument.FileName = consentFileName;
                }
                catch (System.Exception ex)
                {
                    // If upload fails, return error immediately (don't proceed to DB)
                    System.Diagnostics.Trace.TraceError(
                        string.Format("Consent upload failed for Requirement={0}, Bank={1}: {2}",
                            request.RequirementId,
                            request.BankId,
                            ex.Message));

                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Failed to upload consent document to storage",
                        Error = ex.Message
                    };
                }
            }
            else
            {
                System.Diagnostics.Trace.TraceInformation(
                    string.Format("Using pre-uploaded consent file name for Requirement={0}, Bank={1}, FileName={2}",
                        request.RequirementId,
                        request.BankId,
                        request.ConsentDocument.FileName));
            }

            // Now call DB with only the file name (SP_BANK_SUBMIT_QUOTE expects p_consent_file_name)
            return _repo.Bank_SubmitQuote(request);
        }

        public ApiResponse GetConsentDocument(string quoteId, string requirementId, string bankId)
        {
            try
            {
                // First get the consent file name from DB
                var quoteResponse = _repo.Bank_GetQuotes(bankId, requirementId);
                if (!quoteResponse.Success)
                {
                    return quoteResponse;
                }

                // Extract consent file name from quote data
                var consentFileName = ExtractConsentFileName(quoteResponse.Data, quoteId);
                if (string.IsNullOrWhiteSpace(consentFileName))
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Consent document not found for this quote"
                    };
                }

                // Download from FTP
                var base64Content = _ftpStorage.DownloadConsentDocument(consentFileName, requirementId, bankId);
                if (base64Content == null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Consent document file not found on storage server"
                    };
                }

                return new ApiResponse
                {
                    Success = true,
                    Message = "Consent document retrieved successfully",
                    Data = new
                    {
                        FileName = consentFileName,
                        FileData = base64Content,
                        ContentType = "application/pdf"
                    }
                };
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    string.Format("Error retrieving consent document for Quote={0}: {1}", quoteId, ex.Message));
                return new ApiResponse
                {
                    Success = false,
                    Message = "Failed to retrieve consent document",
                    Error = ex.Message
                };
            }
        }

        private string ExtractConsentFileName(object data, string quoteId)
        {
            try
            {
                var dt = data as System.Data.DataTable;
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (System.Data.DataRow row in dt.Rows)
                    {
                        var id = row["ID"] != null ? row["ID"].ToString() : string.Empty;
                        if (id == quoteId && dt.Columns.Contains("CONSENT_FILE_NAME"))
                        {
                            return row["CONSENT_FILE_NAME"] != null && row["CONSENT_FILE_NAME"] != System.DBNull.Value 
                                ? row["CONSENT_FILE_NAME"].ToString() 
                                : null;
                        }
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }

    public class AccountService : IAccountService
    {
        private readonly IDepositRepository _repo;
        private readonly IFtpStorageService _ftpStorage;

        public AccountService(IDepositRepository repo, IFtpStorageService ftpStorage)
        {
            _repo = repo;
            _ftpStorage = ftpStorage;
        }

        public ApiResponse GetRequirements(string status, string depositType) => _repo.Account_GetRequirements(status, depositType);
        public ApiResponse GetRequirementById(string id) => _repo.Common_GetRequirementById(id);
        public ApiResponse CreateRequirement(CreateRequirementRequest request) => _repo.Account_CreateRequirement(request);
        public ApiResponse PublishRequirement(string requirementId, string authorizedBy) => _repo.Account_PublishRequirement(requirementId, authorizedBy);
        public ApiResponse DeleteRequirement(string requirementId) => _repo.Account_DeleteRequirement(requirementId);
        public ApiResponse GetBanks(string status) => _repo.Account_GetBanks(status);
        public ApiResponse CreateBank(CreateBankRequest request) => _repo.Account_CreateBank(request);
        public ApiResponse GetQuotes(string requirementId) => _repo.Account_GetQuotes(requirementId);
        public ApiResponse GetDashboardStats() => _repo.Account_GetDashboardStats();

        public ApiResponse GetConsentDocument(string quoteId, string requirementId, string bankId)
        {
            try
            {
                // First get the consent file name from DB
                var quoteResponse = _repo.Account_GetQuotes(requirementId);
                if (!quoteResponse.Success)
                {
                    return quoteResponse;
                }

                // Extract consent file name from quote data
                var consentFileName = ExtractConsentFileName(quoteResponse.Data, quoteId);
                if (string.IsNullOrWhiteSpace(consentFileName))
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Consent document not found for this quote"
                    };
                }

                // Download from FTP
                var base64Content = _ftpStorage.DownloadConsentDocument(consentFileName, requirementId, bankId);
                if (base64Content == null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Consent document file not found on storage server"
                    };
                }

                return new ApiResponse
                {
                    Success = true,
                    Message = "Consent document retrieved successfully",
                    Data = new
                    {
                        FileName = consentFileName,
                        FileData = base64Content,
                        ContentType = "application/pdf"
                    }
                };
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    string.Format("Error retrieving consent document for Quote={0}: {1}", quoteId, ex.Message));
                return new ApiResponse
                {
                    Success = false,
                    Message = "Failed to retrieve consent document",
                    Error = ex.Message
                };
            }
        }

        private string ExtractConsentFileName(object data, string quoteId)
        {
            try
            {
                var dt = data as System.Data.DataTable;
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (System.Data.DataRow row in dt.Rows)
                    {
                        var id = row["ID"] != null ? row["ID"].ToString() : string.Empty;
                        if (id == quoteId && dt.Columns.Contains("CONSENT_FILE_NAME"))
                        {
                            return row["CONSENT_FILE_NAME"] != null && row["CONSENT_FILE_NAME"] != System.DBNull.Value 
                                ? row["CONSENT_FILE_NAME"].ToString() 
                                : null;
                        }
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }

    public class CommissionerService : ICommissionerService
    {
        private readonly IDepositRepository _repo;
        private readonly IFtpStorageService _ftpStorage;

        public CommissionerService(IDepositRepository repo, IFtpStorageService ftpStorage)
        {
            _repo = repo;
            _ftpStorage = ftpStorage;
        }

        public ApiResponse GetRequirements(string status) => _repo.Commissioner_GetRequirements(status);
        public ApiResponse GetRequirementWithQuotes(string requirementId) => _repo.Commissioner_GetRequirementWithQuotes(requirementId);
        public ApiResponse GetQuotes(string requirementId) => _repo.Commissioner_GetQuotes(requirementId);
        public ApiResponse AuthorizeRequirement(string requirementId, string commissionerId) => _repo.Commissioner_AuthorizeRequirement(requirementId, commissionerId);
        public ApiResponse FinalizeDeposit(string requirementId, string bankId) => _repo.Commissioner_FinalizeDeposit(requirementId, bankId);
        public ApiResponse GetDashboardStats() => _repo.Commissioner_GetDashboardStats();
        public ApiResponse GetEnhancedKpis(string bankId, string depositType, System.DateTime? fromDate, System.DateTime? toDate)
            => _repo.Commissioner_GetEnhancedKpis(bankId, depositType, fromDate, toDate);
        public ApiResponse GetBankWiseAnalytics(string depositType, System.DateTime? fromDate, System.DateTime? toDate)
            => _repo.Commissioner_GetBankWiseAnalytics(depositType, fromDate, toDate);
        public ApiResponse GetUpcomingMaturities(int? withinDays, string bankId, string depositType, int? minDaysLeft, int? maxDaysLeft, string schemeName)
            => _repo.Commissioner_GetUpcomingMaturities(withinDays, bankId, depositType, minDaysLeft, maxDaysLeft, schemeName);
        public ApiResponse GetDepositTypeDistribution(string bankId, System.DateTime? fromDate, System.DateTime? toDate)
            => _repo.Commissioner_GetDepositTypeDistribution(bankId, fromDate, toDate);
        public ApiResponse GetInterestTimeline(int? monthsAhead, string bankId, string depositType, System.DateTime? fromDate, System.DateTime? toDate)
            => _repo.Commissioner_GetInterestTimeline(monthsAhead, bankId, depositType, fromDate, toDate);
        public ApiResponse GetPortfolioHealth(int? minDiversifiedBanks, decimal? maxSingleBankPercent, decimal? targetAvgRate, string bankId, string depositType, System.DateTime? fromDate, System.DateTime? toDate)
            => _repo.Commissioner_GetPortfolioHealth(minDiversifiedBanks, maxSingleBankPercent, targetAvgRate, bankId, depositType, fromDate, toDate);

        public ApiResponse GetConsentDocument(string quoteId, string requirementId, string bankId)
        {
            try
            {
                // First get the consent file name from DB
                var quoteResponse = _repo.Commissioner_GetQuotes(requirementId);
                if (!quoteResponse.Success)
                {
                    return quoteResponse;
                }

                // Extract consent file name from quote data
                var consentFileName = ExtractConsentFileName(quoteResponse.Data, quoteId);
                if (string.IsNullOrWhiteSpace(consentFileName))
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Consent document not found for this quote"
                    };
                }

                // Download from FTP
                var base64Content = _ftpStorage.DownloadConsentDocument(consentFileName, requirementId, bankId);
                if (base64Content == null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Consent document file not found on storage server"
                    };
                }

                return new ApiResponse
                {
                    Success = true,
                    Message = "Consent document retrieved successfully",
                    Data = new
                    {
                        FileName = consentFileName,
                        FileData = base64Content,
                        ContentType = "application/pdf"
                    }
                };
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    string.Format("Error retrieving consent document for Quote={0}: {1}", quoteId, ex.Message));
                return new ApiResponse
                {
                    Success = false,
                    Message = "Failed to retrieve consent document",
                    Error = ex.Message
                };
            }
        }

        private string ExtractConsentFileName(object data, string quoteId)
        {
            try
            {
                var dt = data as System.Data.DataTable;
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (System.Data.DataRow row in dt.Rows)
                    {
                        var id = row["ID"] != null ? row["ID"].ToString() : string.Empty;
                        if (id == quoteId && dt.Columns.Contains("CONSENT_FILE_NAME"))
                        {
                            return row["CONSENT_FILE_NAME"] != null && row["CONSENT_FILE_NAME"] != System.DBNull.Value 
                                ? row["CONSENT_FILE_NAME"].ToString() 
                                : null;
                        }
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
