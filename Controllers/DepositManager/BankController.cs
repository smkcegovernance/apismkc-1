using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using SmkcApi.Services.DepositManager;
using SmkcApi.Models.DepositManager;
using SmkcApi.Security;

namespace SmkcApi.Controllers.DepositManager
{
    /// <summary>
    /// Bank Controller for Deposit Manager
    /// Handles bank-side operations for viewing requirements and submitting quotes
    /// </summary>
    [RoutePrefix("api/deposits/bank")]
    [ShaAuthentication]
    [RateLimit(maxRequests: 100, timeWindowMinutes: 1)]
    public class BankController : ApiController
    {
        private readonly IBankService _service;
        
        public BankController(IBankService service) 
        { 
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Get published deposit requirements
        /// GET: /api/deposits/bank/requirements
        /// </summary>
        [HttpGet]
        [Route("requirements")]
        public HttpResponseMessage GetRequirements(string status = "published", string depositType = null)
        {
            try
            {
                var res = _service.GetRequirements(status, depositType);
                LogRequest("GetRequirements", res.Success);
                return Request.CreateResponse(res.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest, res);
            }
            catch (Exception ex)
            {
                LogError("GetRequirements", ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, 
                    new ApiResponse { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        /// <summary>
        /// Get specific requirement details by ID
        /// GET: /api/deposits/bank/requirements/{id}
        /// </summary>
        [HttpGet]
        [Route("requirements/{id}")]
        public HttpResponseMessage GetRequirementById(string id)
        {
            try
            {
                // Enhanced logging for debugging
                var requestUri = Request.RequestUri?.PathAndQuery;
                LogRequest($"GetRequirementById - URI: {requestUri}, ID Parameter: {id}", true);
                
                // Validate ID parameter
                if (string.IsNullOrWhiteSpace(id))
                {
                    LogRequest("GetRequirementById - Invalid ID", false);
                    return Request.CreateResponse(HttpStatusCode.BadRequest, 
                        new ApiResponse 
                        { 
                            Success = false, 
                            Message = "Requirement ID is required and cannot be empty",
                            Error = "INVALID_PARAMETER"
                        });
                }

                // Check for common variable resolution issues
                if (id.Contains("{{") || id.Contains("}}"))
                {
                    LogRequest($"GetRequirementById - Unresolved variable detected: {id}", false);
                    return Request.CreateResponse(HttpStatusCode.BadRequest, 
                        new ApiResponse 
                        { 
                            Success = false, 
                            Message = "Invalid requirement ID format. Please ensure Postman environment variable is resolved.",
                            Error = "UNRESOLVED_VARIABLE",
                            Data = new { providedId = id, hint = "Check if {{requirementId}} is being replaced with actual value" }
                        });
                }
                
                var res = _service.GetRequirementById(id);
                LogRequest($"GetRequirementById - ID: {id}, Success: {res.Success}", res.Success);
                
                // Return NotFound instead of generic error for better clarity
                var statusCode = res.Success ? HttpStatusCode.OK : HttpStatusCode.NotFound;
                return Request.CreateResponse(statusCode, res);
            }
            catch (Exception ex)
            {
                LogError($"GetRequirementById - ID: {id}", ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, 
                    new ApiResponse 
                    { 
                        Success = false, 
                        Message = "An error occurred while processing your request",
                        Error = ex.Message 
                    });
            }
        }

        /// <summary>
        /// Get quotes submitted by the bank
        /// GET: /api/deposits/bank/quotes
        /// </summary>
        [HttpGet]
        [Route("quotes")]
        public HttpResponseMessage GetQuotes(string bankId, string requirementId = null)
        {
            try
            {
                var res = _service.GetQuotes(bankId, requirementId);
                LogRequest("GetQuotes", res.Success);
                return Request.CreateResponse(res.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest, res);
            }
            catch (Exception ex)
            {
                LogError("GetQuotes", ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, 
                    new ApiResponse { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        /// <summary>
        /// Get dashboard statistics for bank
        /// GET: /api/deposits/bank/dashboard/stats
        /// </summary>
        [HttpGet]
        [Route("dashboard/stats")]
        public HttpResponseMessage GetDashboardStats(string bankId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(bankId))
                {
                    LogRequest("GetDashboardStats - Invalid bankId", false);
                    return Request.CreateResponse(HttpStatusCode.BadRequest, 
                        new ApiResponse 
                        { 
                            Success = false, 
                            Message = "Bank ID is required",
                            Error = "INVALID_PARAMETER"
                        });
                }

                var res = _service.GetDashboardStats(bankId);
                LogRequest($"GetDashboardStats - BankId: {bankId}", res.Success);
                return Request.CreateResponse(res.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest, res);
            }
            catch (Exception ex)
            {
                LogError("GetDashboardStats", ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, 
                    new ApiResponse { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        /// <summary>
        /// Submit a quote for a requirement
        /// POST: /api/deposits/bank/quotes/submit
        /// </summary>
        [HttpPost]
        [Route("quotes/submit")]
        public HttpResponseMessage SubmitQuote(SubmitQuoteRequest req)
        {
            try
            {
                var val = req?.Validate();
                if (val == null || !val.Success)
                {
                    LogRequest("SubmitQuote", false);
                    return Request.CreateResponse(HttpStatusCode.BadRequest, 
                        val ?? new ApiResponse { Success = false, Message = "Invalid request" });
                }
                
                var res = _service.SubmitQuote(req);
                var status = res.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
                LogRequest("SubmitQuote", res.Success);
                return Request.CreateResponse(status, res);
            }
            catch (Exception ex)
            {
                LogError("SubmitQuote", ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, 
                    new ApiResponse { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        /// <summary>
        /// Download consent document for a specific quote
        /// GET: /api/deposits/bank/quotes/{quoteId}/consent
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
                        new ApiResponse 
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
                
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue(inline ? "inline" : "attachment")
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
                    new ApiResponse 
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

            var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - BANK_CONTROLLER_{action.ToUpper()} - " +
                          $"Status: {(success ? "SUCCESS" : "FAILED")}, ApiKey: {apiKey}, RequestId: {requestId}";
            
            System.Diagnostics.Trace.TraceInformation(logEntry);
        }

        private void LogError(string action, Exception ex)
        {
            var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - BANK_CONTROLLER_{action.ToUpper()}_ERROR: {ex.Message}";
            System.Diagnostics.Trace.TraceError(logEntry);

            if (Request.Properties.ContainsKey("ApiKey"))
            {
                var apiKey = Request.Properties["ApiKey"].ToString();
                var maskedApiKey = SecurityHelper.MaskSensitiveData(apiKey);
                var requestId = Request.Properties.ContainsKey("RequestId") ? Request.Properties["RequestId"].ToString() : "Unknown";

                var securityLogEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - SECURITY_ERROR - Controller: BankController, " +
                                      $"Action: {action}, ApiKey: {maskedApiKey}, RequestId: {requestId}, Error: {ex.Message}";
                System.Diagnostics.Trace.TraceError(securityLogEntry);
            }
        }
    }
}
