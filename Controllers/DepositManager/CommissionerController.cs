using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using SmkcApi.Services.DepositManager;
using SmkcApi.Security;

namespace SmkcApi.Controllers.DepositManager
{
    /// <summary>
    /// Commissioner Controller for Deposit Manager
    /// Handles commissioner-level operations for authorizing and finalizing deposits
    /// </summary>
    [RoutePrefix("api/deposits/commissioner")]
    [ShaAuthentication]
    [RateLimit(maxRequests: 100, timeWindowMinutes: 1)]
    public class CommissionerController : ApiController
    {
        private readonly ICommissionerService _service;
        
        public CommissionerController(ICommissionerService service) 
        { 
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Get all requirements for commissioner review
        /// GET: /api/deposits/commissioner/requirements
        /// </summary>
        [HttpGet]
        [Route("requirements")]
        public HttpResponseMessage GetRequirements(string status = null)
        {
            try
            {
                var res = _service.GetRequirements(status);
                LogRequest("GetRequirements", res.Success);
                return Request.CreateResponse(res.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest, res);
            }
            catch (Exception ex)
            {
                LogError("GetRequirements", ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, 
                    new Models.DepositManager.ApiResponse { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        /// <summary>
        /// Get requirement details with all submitted quotes
        /// GET: /api/deposits/commissioner/requirements/{id}
        /// </summary>
        [HttpGet]
        [Route("requirements/{id}")]
        public HttpResponseMessage GetRequirementWithQuotes(string id)
        {
            try
            {
                var res = _service.GetRequirementWithQuotes(id);
                LogRequest("GetRequirementWithQuotes", res.Success);
                return Request.CreateResponse(res.Success ? HttpStatusCode.OK : HttpStatusCode.NotFound, res);
            }
            catch (Exception ex)
            {
                LogError("GetRequirementWithQuotes", ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, 
                    new Models.DepositManager.ApiResponse { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        /// <summary>
        /// Authorize a requirement for bank submissions
        /// POST: /api/deposits/commissioner/requirements/{id}/authorize
        /// </summary>
        [HttpPost]
        [Route("requirements/{id}/authorize")]
        public HttpResponseMessage Authorize(string id, [FromBody] CommissionerAuthorizeRequest req)
        {
            try
            {
                var res = _service.AuthorizeRequirement(id, req?.CommissionerId);
                LogRequest("Authorize", res.Success);
                return Request.CreateResponse(res.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest, res);
            }
            catch (Exception ex)
            {
                LogError("Authorize", ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, 
                    new Models.DepositManager.ApiResponse { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        /// <summary>
        /// Finalize deposit by selecting winning bank quote
        /// POST: /api/deposits/commissioner/requirements/{id}/finalize
        /// </summary>
        [HttpPost]
        [Route("requirements/{id}/finalize")]
        public HttpResponseMessage Finalize(string id, [FromBody] FinalizeDepositRequest req)
        {
            try
            {
                var res = _service.FinalizeDeposit(id, req?.BankId);
                LogRequest("Finalize", res.Success);
                return Request.CreateResponse(res.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest, res);
            }
            catch (Exception ex)
            {
                LogError("Finalize", ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, 
                    new Models.DepositManager.ApiResponse { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        /// <summary>
        /// Get all quotes for commissioner review
        /// GET: /api/deposits/commissioner/quotes
        /// </summary>
        [HttpGet]
        [Route("quotes")]
        public HttpResponseMessage GetQuotes(string requirementId = null)
        {
            try
            {
                var res = _service.GetQuotes(requirementId);
                LogRequest("GetQuotes", res.Success);
                return Request.CreateResponse(res.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest, res);
            }
            catch (Exception ex)
            {
                LogError("GetQuotes", ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, 
                    new Models.DepositManager.ApiResponse { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        /// <summary>
        /// Get dashboard statistics for commissioner
        /// GET: /api/deposits/commissioner/dashboard/stats
        /// </summary>
        [HttpGet]
        [Route("dashboard/stats")]
        public HttpResponseMessage GetDashboardStats()
        {
            try
            {
                var res = _service.GetDashboardStats();
                LogRequest("GetDashboardStats", res.Success);
                return Request.CreateResponse(res.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest, res);
            }
            catch (Exception ex)
            {
                LogError("GetDashboardStats", ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, 
                    new Models.DepositManager.ApiResponse { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        /// <summary>
        /// Get enhanced KPI cards data for commissioner dashboard
        /// GET: /api/deposits/commissioner/dashboard/enhanced-kpis
        /// </summary>
        [HttpGet]
        [Route("dashboard/enhanced-kpis")]
        public HttpResponseMessage GetEnhancedKpis(string bankId = null, string depositType = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var res = _service.GetEnhancedKpis(bankId, depositType, fromDate, toDate);
                LogRequest("GetEnhancedKpis", res.Success);
                return Request.CreateResponse(res.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest, res);
            }
            catch (Exception ex)
            {
                LogError("GetEnhancedKpis", ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError,
                    new Models.DepositManager.ApiResponse { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        /// <summary>
        /// Get bank-wise investment and interest analytics
        /// GET: /api/deposits/commissioner/dashboard/bank-wise-analytics
        /// </summary>
        [HttpGet]
        [Route("dashboard/bank-wise-analytics")]
        public HttpResponseMessage GetBankWiseAnalytics(string depositType = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var res = _service.GetBankWiseAnalytics(depositType, fromDate, toDate);
                LogRequest("GetBankWiseAnalytics", res.Success);
                return Request.CreateResponse(res.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest, res);
            }
            catch (Exception ex)
            {
                LogError("GetBankWiseAnalytics", ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError,
                    new Models.DepositManager.ApiResponse { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        /// <summary>
        /// Get upcoming maturity list with day-window and optional filters
        /// GET: /api/deposits/commissioner/dashboard/upcoming-maturities
        /// </summary>
        [HttpGet]
        [Route("dashboard/upcoming-maturities")]
        public HttpResponseMessage GetUpcomingMaturities(
            int? withinDays = 90,
            string bankId = null,
            string depositType = null,
            int? minDaysLeft = null,
            int? maxDaysLeft = null,
            string schemeName = null)
        {
            try
            {
                var res = _service.GetUpcomingMaturities(withinDays, bankId, depositType, minDaysLeft, maxDaysLeft, schemeName);
                LogRequest("GetUpcomingMaturities", res.Success);
                return Request.CreateResponse(res.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest, res);
            }
            catch (Exception ex)
            {
                LogError("GetUpcomingMaturities", ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError,
                    new Models.DepositManager.ApiResponse { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        /// <summary>
        /// Get callable vs non-callable distribution for dashboard pie chart
        /// GET: /api/deposits/commissioner/dashboard/deposit-type-distribution
        /// </summary>
        [HttpGet]
        [Route("dashboard/deposit-type-distribution")]
        public HttpResponseMessage GetDepositTypeDistribution(string bankId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var res = _service.GetDepositTypeDistribution(bankId, fromDate, toDate);
                LogRequest("GetDepositTypeDistribution", res.Success);
                return Request.CreateResponse(res.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest, res);
            }
            catch (Exception ex)
            {
                LogError("GetDepositTypeDistribution", ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError,
                    new Models.DepositManager.ApiResponse { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        /// <summary>
        /// Get projected interest payout timeline by month
        /// GET: /api/deposits/commissioner/dashboard/interest-timeline
        /// </summary>
        [HttpGet]
        [Route("dashboard/interest-timeline")]
        public HttpResponseMessage GetInterestTimeline(
            int? monthsAhead = 12,
            string bankId = null,
            string depositType = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            try
            {
                var res = _service.GetInterestTimeline(monthsAhead, bankId, depositType, fromDate, toDate);
                LogRequest("GetInterestTimeline", res.Success);
                return Request.CreateResponse(res.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest, res);
            }
            catch (Exception ex)
            {
                LogError("GetInterestTimeline", ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError,
                    new Models.DepositManager.ApiResponse { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        /// <summary>
        /// Get portfolio health score and metric components
        /// GET: /api/deposits/commissioner/dashboard/portfolio-health
        /// </summary>
        [HttpGet]
        [Route("dashboard/portfolio-health")]
        public HttpResponseMessage GetPortfolioHealth(
            int? minDiversifiedBanks = 5,
            decimal? maxSingleBankPercent = 40,
            decimal? targetAvgRate = 7,
            string bankId = null,
            string depositType = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            try
            {
                var res = _service.GetPortfolioHealth(minDiversifiedBanks, maxSingleBankPercent, targetAvgRate, bankId, depositType, fromDate, toDate);
                LogRequest("GetPortfolioHealth", res.Success);
                return Request.CreateResponse(res.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest, res);
            }
            catch (Exception ex)
            {
                LogError("GetPortfolioHealth", ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError,
                    new Models.DepositManager.ApiResponse { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        /// <summary>
        /// Download consent document for a specific quote
        /// GET: /api/deposits/commissioner/quotes/{quoteId}/consent
        /// </summary>
        [HttpGet]
        [Route("quotes/{quoteId}/consent")]
        public HttpResponseMessage DownloadConsentDocument(string quoteId, string requirementId, string bankId, bool inline = false)
        {
            try
            {
                // Validate parameters
                if (string.IsNullOrWhiteSpace(quoteId) || 
                    string.IsNullOrWhiteSpace(requirementId) || 
                    string.IsNullOrWhiteSpace(bankId))
                {
                    LogRequest("DownloadConsentDocument - Missing parameters", false);
                    return Request.CreateResponse(HttpStatusCode.BadRequest, 
                        new Models.DepositManager.ApiResponse 
                        { 
                            Success = false, 
                            Message = "quoteId, requirementId, and bankId are required",
                            Error = "INVALID_PARAMETER"
                        });
                }

                var res = _service.GetConsentDocument(quoteId, requirementId, bankId);
                
                if (!res.Success)
                {
                    LogRequest($"DownloadConsentDocument - Failed for Quote: {quoteId}", false);
                    var statusCode = res.Message != null && res.Message.Contains("not found") 
                        ? HttpStatusCode.NotFound 
                        : HttpStatusCode.BadRequest;
                    return Request.CreateResponse(statusCode, res);
                }

                // Extract file data from response
                dynamic data = res.Data;
                var fileName = data.FileName;
                var base64Content = data.FileData;

                // Return as binary file download
                var fileBytes = Convert.FromBase64String(base64Content);
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(fileBytes)
                };
                
                response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
                response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue(inline ? "inline" : "attachment")
                {
                    FileName = fileName
                };
                response.Content.Headers.ContentLength = fileBytes.Length;

                LogRequest($"DownloadConsentDocument - Success: {fileName}", true);
                return response;
            }
            catch (Exception ex)
            {
                LogError($"DownloadConsentDocument - Quote: {quoteId}", ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, 
                    new Models.DepositManager.ApiResponse 
                    { 
                        Success = false, 
                        Message = "An error occurred while downloading consent document",
                        Error = ex.Message 
                    });
            }
        }

        private void LogRequest(string action, bool success)
        {
            var apiKey = Request.Properties.ContainsKey("ApiKey") 
                ? SecurityHelper.MaskSensitiveData(Request.Properties["ApiKey"].ToString()) 
                : "Unknown";
            var requestId = Request.Properties.ContainsKey("RequestId") 
                ? Request.Properties["RequestId"].ToString() 
                : "Unknown";

            var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - COMMISSIONER_CONTROLLER_{action.ToUpper()} - " +
                          $"Status: {(success ? "SUCCESS" : "FAILED")}, ApiKey: {apiKey}, RequestId: {requestId}";
            
            System.Diagnostics.Trace.TraceInformation(logEntry);
        }

        private void LogError(string action, Exception ex)
        {
            var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - COMMISSIONER_CONTROLLER_{action.ToUpper()}_ERROR: {ex.Message}";
            System.Diagnostics.Trace.TraceError(logEntry);

            if (Request.Properties.ContainsKey("ApiKey"))
            {
                var apiKey = Request.Properties["ApiKey"].ToString();
                var maskedApiKey = SecurityHelper.MaskSensitiveData(apiKey);
                var requestId = Request.Properties.ContainsKey("RequestId") ? Request.Properties["RequestId"].ToString() : "Unknown";

                var securityLogEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - SECURITY_ERROR - Controller: CommissionerController, " +
                                      $"Action: {action}, ApiKey: {maskedApiKey}, RequestId: {requestId}, Error: {ex.Message}";
                System.Diagnostics.Trace.TraceError(securityLogEntry);
            }
        }
    }

    /// <summary>
    /// Request model for commissioner authorization
    /// </summary>
    public class CommissionerAuthorizeRequest 
    { 
        public string CommissionerId { get; set; } 
    }

    /// <summary>
    /// Request model for finalizing deposit with selected bank
    /// </summary>
    public class FinalizeDepositRequest 
    { 
        public string BankId { get; set; } 
    }
}
