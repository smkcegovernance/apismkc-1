using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using SmkcApi.Services.DepositManager;
using SmkcApi.Models.DepositManager;
using SmkcApi.Security;

namespace SmkcApi.Controllers.DepositManager
{
    /// <summary>
    /// Account Controller for Deposit Manager
    /// Handles account department operations for requirements, banks, and quotes
    /// </summary>
    [RoutePrefix("api/deposits/account")]
    [ShaAuthentication]
    [RateLimit(maxRequests: 100, timeWindowMinutes: 1)]
    public class AccountController : ApiController
    {
        private readonly IAccountService _service;
        
        public AccountController(IAccountService service) 
        { 
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Get all deposit requirements
        /// GET: /api/deposits/account/requirements
        /// </summary>
        [HttpGet]
        [Route("requirements")]
        public HttpResponseMessage GetRequirements(string status = null, string depositType = null)
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
        /// GET: /api/deposits/account/requirements/{id}
        /// </summary>
        [HttpGet]
        [Route("requirements/{id}")]
        public HttpResponseMessage GetRequirementById(string id)
        {
            try
            {
                // Enhanced logging for debugging
                var requestUri = Request.RequestUri?.PathAndQuery;
                LogRequest(string.Format("GetRequirementById - URI: {0}, ID Parameter: {1}", requestUri, id), true);
                
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
                if (id.IndexOf("{{", StringComparison.Ordinal) >= 0 || id.IndexOf("}}", StringComparison.Ordinal) >= 0)
                {
                    LogRequest(string.Format("GetRequirementById - Unresolved variable detected: {0}", id), false);
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
                LogRequest(string.Format("GetRequirementById - ID: {0}, Success: {1}", id, res.Success), res.Success);
                
                // Return NotFound instead of generic error for better clarity
                var statusCode = res.Success ? HttpStatusCode.OK : HttpStatusCode.NotFound;
                return Request.CreateResponse(statusCode, res);
            }
            catch (Exception ex)
            {
                LogError(string.Format("GetRequirementById - ID: {0}", id), ex);
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
        /// Create a new deposit requirement
        /// POST: /api/deposits/account/requirements
        /// </summary>
        [HttpPost]
        [Route("requirements")]
        public HttpResponseMessage CreateRequirement(CreateRequirementRequest req)
        {
            try
            {
                var val = req?.Validate();
                if (val == null || !val.Success)
                {
                    LogRequest("CreateRequirement - Validation Failed", false);
                    return Request.CreateResponse(HttpStatusCode.BadRequest, 
                        val ?? new ApiResponse { Success = false, Message = "Invalid request" });
                }
                
                var res = _service.CreateRequirement(req);
                
                if (!res.Success)
                {
                    LogRequest("CreateRequirement - Failed", false);
                    
                    // Check for duplicate (409 Conflict)
                    if (!string.IsNullOrEmpty(res.Error) && res.Error.ToLower().IndexOf("duplicate") >= 0)
                    {
                        return Request.CreateResponse(HttpStatusCode.Conflict, res);
                    }
                    
                    return Request.CreateResponse(HttpStatusCode.BadRequest, res);
                }
                
                LogRequest("CreateRequirement - Success", true);
                
                // Return 201 Created with Location header
                var response = Request.CreateResponse(HttpStatusCode.Created, res);
                
                // Extract requirement ID from response data
                if (res.Data != null)
                {
                    var requirementId = ExtractRequirementId(res.Data);
                    if (!string.IsNullOrEmpty(requirementId))
                    {
                        var locationUri = new Uri(Request.RequestUri, "/api/deposits/account/requirements/" + requirementId);
                        response.Headers.Location = locationUri;
                    }
                }
                
                return response;
            }
            catch (Exception ex)
            {
                LogError("CreateRequirement", ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, 
                    new ApiResponse { Success = false, Message = "An error occurred while processing your request", Error = ex.Message });
            }
        }

        /// <summary>
        /// Publish a requirement (change status from draft to published)
        /// POST: /api/deposits/account/requirements/{id}/publish
        /// </summary>
        [HttpPost]
        [Route("requirements/{id}/publish")]
        public HttpResponseMessage PublishRequirement(string id, [FromBody] PublishRequirementRequest req)
        {
            try
            {
                // Validate ID parameter
                if (string.IsNullOrWhiteSpace(id))
                {
                    LogRequest("PublishRequirement - Invalid ID", false);
                    return Request.CreateResponse(HttpStatusCode.BadRequest, 
                        new ApiResponse 
                        { 
                            Success = false, 
                            Message = "Requirement ID is required",
                            Error = "INVALID_PARAMETER"
                        });
                }

                // Validate request body
                if (req == null || string.IsNullOrWhiteSpace(req.AuthorizedBy))
                {
                    LogRequest("PublishRequirement - Missing authorizedBy", false);
                    return Request.CreateResponse(HttpStatusCode.BadRequest, 
                        new ApiResponse 
                        { 
                            Success = false, 
                            Message = "authorizedBy is required",
                            Error = "INVALID_REQUEST"
                        });
                }
                
                var res = _service.PublishRequirement(id, req.AuthorizedBy);
                
                if (!res.Success)
                {
                    LogRequest(string.Format("PublishRequirement - Failed for ID: {0}", id), false);
                    
                    // Check for not found (404)
                    if (!string.IsNullOrEmpty(res.Message) && res.Message.ToLower().IndexOf("not found") >= 0)
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, res);
                    }
                    
                    // Check for invalid state (409 Conflict)
                    if (!string.IsNullOrEmpty(res.Message) && 
                        (res.Message.ToLower().IndexOf("not in draft") >= 0 ||
                         res.Message.ToLower().IndexOf("already published") >= 0 ||
                         res.Message.ToLower().IndexOf("invalid status") >= 0))
                    {
                        return Request.CreateResponse(HttpStatusCode.Conflict, res);
                    }
                    
                    return Request.CreateResponse(HttpStatusCode.BadRequest, res);
                }
                
                LogRequest(string.Format("PublishRequirement - Success for ID: {0}", id), true);
                
                // Return 204 No Content on success
                return Request.CreateResponse(HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                LogError(string.Format("PublishRequirement - ID: {0}", id), ex);
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
        /// Delete a requirement (soft delete - only allowed from draft status)
        /// DELETE: /api/deposits/account/requirements/{id}
        /// </summary>
        [HttpDelete]
        [Route("requirements/{id}")]
        public HttpResponseMessage DeleteRequirement(string id)
        {
            try
            {
                // Validate ID parameter
                if (string.IsNullOrWhiteSpace(id))
                {
                    LogRequest("DeleteRequirement - Invalid ID", false);
                    return Request.CreateResponse(HttpStatusCode.BadRequest, 
                        new ApiResponse 
                        { 
                            Success = false, 
                            Message = "Requirement ID is required",
                            Error = "INVALID_PARAMETER"
                        });
                }
                
                var res = _service.DeleteRequirement(id);
                
                if (!res.Success)
                {
                    LogRequest(string.Format("DeleteRequirement - Failed for ID: {0}", id), false);
                    
                    // Check for not found (404)
                    if (!string.IsNullOrEmpty(res.Message) && res.Message.ToLower().IndexOf("not found") >= 0)
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, res);
                    }
                    
                    // Check for invalid state (409 Conflict)
                    if (!string.IsNullOrEmpty(res.Message) && 
                        (res.Message.ToLower().IndexOf("not in draft") >= 0 ||
                         res.Message.ToLower().IndexOf("cannot be deleted") >= 0 ||
                         res.Message.ToLower().IndexOf("invalid status") >= 0))
                    {
                        return Request.CreateResponse(HttpStatusCode.Conflict, res);
                    }
                    
                    return Request.CreateResponse(HttpStatusCode.BadRequest, res);
                }
                
                LogRequest(string.Format("DeleteRequirement - Success for ID: {0}", id), true);
                
                // Return 204 No Content on success
                return Request.CreateResponse(HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                LogError(string.Format("DeleteRequirement - ID: {0}", id), ex);
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
        /// Get all registered banks
        /// GET: /api/deposits/account/banks
        /// </summary>
        [HttpGet]
        [Route("banks")]
        public HttpResponseMessage GetBanks(string status = null)
        {
            try
            {
                var res = _service.GetBanks(status);
                LogRequest("GetBanks", res.Success);
                return Request.CreateResponse(res.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest, res);
            }
            catch (Exception ex)
            {
                LogError("GetBanks", ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, 
                    new ApiResponse { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        /// <summary>
        /// Register a new bank
        /// POST: /api/deposits/account/banks/create
        /// </summary>
        [HttpPost]
        [Route("banks/create")]
        public HttpResponseMessage CreateBank(CreateBankRequest req)
        {
            try
            {
                var val = req?.Validate();
                if (val == null || !val.Success)
                {
                    LogRequest("CreateBank", false);
                    return Request.CreateResponse(HttpStatusCode.BadRequest, 
                        val ?? new ApiResponse { Success = false, Message = "Invalid request" });
                }
                
                var res = _service.CreateBank(req);
                LogRequest("CreateBank", res.Success);
                return Request.CreateResponse(res.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest, res);
            }
            catch (Exception ex)
            {
                LogError("CreateBank", ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, 
                    new ApiResponse { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        /// <summary>
        /// Get all quotes
        /// GET: /api/deposits/account/quotes
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
                    new ApiResponse { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        /// <summary>
        /// Get dashboard statistics for account department
        /// GET: /api/deposits/account/dashboard/stats
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
                    new ApiResponse { Success = false, Message = "An error occurred while processing your request" });
            }
        }

        /// <summary>
        /// Download consent document for a specific quote
        /// GET: /api/deposits/account/quotes/{quoteId}/consent
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
                    LogRequest(string.Format("DownloadConsentDocument - Failed for Quote: {0}", quoteId), false);
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

                LogRequest(string.Format("DownloadConsentDocument - Success: {0}", fileName), true);
                return response;
            }
            catch (Exception ex)
            {
                LogError(string.Format("DownloadConsentDocument - Quote: {0}", quoteId), ex);
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

            var logEntry = string.Format("{0:yyyy-MM-dd HH:mm:ss} UTC - ACCOUNT_CONTROLLER_{1} - Status: {2}, ApiKey: {3}, RequestId: {4}",
                DateTime.UtcNow,
                action.ToUpper(),
                success ? "SUCCESS" : "FAILED",
                apiKey,
                requestId);
            
            System.Diagnostics.Trace.TraceInformation(logEntry);
        }

        private void LogError(string action, Exception ex)
        {
            var logEntry = string.Format("{0:yyyy-MM-dd HH:mm:ss} UTC - ACCOUNT_CONTROLLER_{1}_ERROR: {2}",
                DateTime.UtcNow,
                action.ToUpper(),
                ex.Message);
            System.Diagnostics.Trace.TraceError(logEntry);

            if (Request.Properties.ContainsKey("ApiKey"))
            {
                var apiKey = Request.Properties["ApiKey"].ToString();
                var maskedApiKey = SecurityHelper.MaskSensitiveData(apiKey);
                var requestId = Request.Properties.ContainsKey("RequestId") ? Request.Properties["RequestId"].ToString() : "Unknown";

                var securityLogEntry = string.Format("{0:yyyy-MM-dd HH:mm:ss} UTC - SECURITY_ERROR - Controller: AccountController, Action: {1}, ApiKey: {2}, RequestId: {3}, Error: {4}",
                    DateTime.UtcNow,
                    action,
                    maskedApiKey,
                    requestId,
                    ex.Message);
                System.Diagnostics.Trace.TraceError(securityLogEntry);
            }
        }

        private string ExtractRequirementId(object data)
        {
            try
            {
                if (data == null) return null;

                // Handle DataTable
                var dt = data as System.Data.DataTable;
                if (dt != null && dt.Rows.Count > 0)
                {
                    // Try common column names
                    if (dt.Columns.Contains("REQUIREMENT_ID"))
                        return dt.Rows[0]["REQUIREMENT_ID"] != null ? dt.Rows[0]["REQUIREMENT_ID"].ToString() : null;
                    if (dt.Columns.Contains("ID"))
                        return dt.Rows[0]["ID"] != null ? dt.Rows[0]["ID"].ToString() : null;
                    if (dt.Columns.Contains("RequirementId"))
                        return dt.Rows[0]["RequirementId"] != null ? dt.Rows[0]["RequirementId"].ToString() : null;
                }

                // Handle Dictionary
                var dict = data as System.Collections.Generic.Dictionary<string, object>;
                if (dict != null)
                {
                    foreach (var kv in dict)
                    {
                        var table = kv.Value as System.Data.DataTable;
                        if (table != null && table.Rows.Count > 0)
                        {
                            if (table.Columns.Contains("REQUIREMENT_ID"))
                                return table.Rows[0]["REQUIREMENT_ID"] != null ? table.Rows[0]["REQUIREMENT_ID"].ToString() : null;
                            if (table.Columns.Contains("ID"))
                                return table.Rows[0]["ID"] != null ? table.Rows[0]["ID"].ToString() : null;
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
