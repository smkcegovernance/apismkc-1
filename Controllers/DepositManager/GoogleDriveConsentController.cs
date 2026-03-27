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
    /// Google Drive Consent Document Controller for Deposit Manager
    /// Provides API endpoints for uploading and downloading bank consent documents using Google Drive
    /// Accessible by all roles (Account, Bank, Commissioner)
    /// NO AUTHENTICATION REQUIRED - Plain access for all users
    /// </summary>
    [RoutePrefix("api/deposits/consent/googledrive")]
    public class GoogleDriveConsentController : ApiController
    {
        private readonly GoogleDriveStorageService _driveStorage;
        
        public GoogleDriveConsentController() 
        { 
            _driveStorage = new GoogleDriveStorageService();
            
            // Ensure FtpLogger is initialized
            FtpLogger.Initialize();
        }

        /// <summary>
        /// Health check endpoint to verify controller is accessible
        /// GET: /api/deposits/consent/googledrive/health
        /// </summary>
        [HttpGet]
        [Route("health")]
        [AllowAnonymous]
        public HttpResponseMessage HealthCheck()
        {
            FtpLogger.LogInfo("=== GOOGLE_DRIVE_CONSENT_CONTROLLER - HealthCheck - Request Received ===");
            
            var response = new
            {
                success = true,
                message = "GoogleDriveConsentController is accessible",
                timestamp = DateTime.UtcNow,
                storageType = "Google Drive",
                storageLocation = "DepositManager/BankConsent",
                logFilePath = FtpLogger.GetLogFilePath(),
                authenticationRequired = false
            };
            
            FtpLogger.LogInfo("GOOGLE_DRIVE_CONSENT_CONTROLLER - HealthCheck - Success");
            
            return Request.CreateResponse(HttpStatusCode.OK, response);
        }

        /// <summary>
        /// Upload/Create a new consent document to Google Drive
        /// POST: /api/deposits/consent/googledrive/upload
        /// NO AUTHENTICATION REQUIRED - Plain access
        /// </summary>
        /// <param name="request">Upload consent document request</param>
        /// <returns>Upload result with file information</returns>
        [HttpPost]
        [Route("upload")]
        [AllowAnonymous]
        public HttpResponseMessage UploadConsentDocument([FromBody] UploadConsentDocumentRequest request)
        {
            var requestId = Guid.NewGuid().ToString();
            var clientIp = GetClientIpAddress();

            try
            {
                // Log request start
                FtpLogger.LogInfo("================================================================================");
                FtpLogger.LogInfo(string.Format("=== GOOGLE DRIVE UPLOAD REQUEST START - RequestId: {0} ===", requestId));
                FtpLogger.LogInfo("================================================================================");
                FtpLogger.LogInfo(string.Format("Client IP: {0}", clientIp));
                FtpLogger.LogInfo(string.Format("RequestId: {0}", requestId));
                FtpLogger.LogInfo(string.Format("Timestamp: {0:yyyy-MM-dd HH:mm:ss.fff} UTC", DateTime.UtcNow));
                FtpLogger.LogInfo(string.Format("Authentication: NONE (Plain Access)"));

                LogRequest(string.Format("UploadConsentDocument - Starting request"), true);

                // Validate request object
                if (request == null)
                {
                    FtpLogger.LogError(string.Format("VALIDATION FAILED - Request body is null [RequestId: {0}]", requestId));
                    LogRequest("UploadConsentDocument - Null request body", false);
                    return Request.CreateResponse(HttpStatusCode.BadRequest,
                        new ApiResponse
                        {
                            Success = false,
                            Message = "Request body is required",
                            Error = "INVALID_REQUEST",
                            ErrorCode = "NULL_REQUEST"
                        });
                }

                FtpLogger.LogInfo("--- Request Parameters ---");
                FtpLogger.LogInfo(string.Format("  RequirementId: {0}", request.RequirementId ?? "(null)"));
                FtpLogger.LogInfo(string.Format("  BankId: {0}", request.BankId ?? "(null)"));
                FtpLogger.LogInfo(string.Format("  QuoteId: {0}", request.QuoteId ?? "(null)"));
                FtpLogger.LogInfo(string.Format("  FileName: {0}", request.FileName ?? "(null)"));
                FtpLogger.LogInfo(string.Format("  FileSize: {0}", request.FileSize?.ToString() ?? "(not provided)"));
                FtpLogger.LogInfo(string.Format("  ContentType: {0}", request.ContentType ?? "(not provided)"));
                FtpLogger.LogInfo(string.Format("  UploadedBy: {0}", request.UploadedBy ?? "(not provided)"));

                // Validate request parameters
                FtpLogger.LogStep(1, "Validating request parameters", true);
                var validationResult = request.Validate();
                
                if (!validationResult.Success)
                {
                    FtpLogger.LogError(string.Format("VALIDATION FAILED - {0} [RequestId: {1}]", validationResult.Message, requestId));
                    LogRequest(string.Format("UploadConsentDocument - Validation failed: {0}", validationResult.Message), false);
                    return Request.CreateResponse(HttpStatusCode.BadRequest, validationResult);
                }

                FtpLogger.LogStep(1, "Validating request parameters", true, "All parameters valid");
                LogRequest("UploadConsentDocument - Parameters validated", true);

                // Create ConsentDocumentDto for storage service
                var consentDocument = new ConsentDocumentDto
                {
                    FileName = request.FileName,
                    FileData = request.FileData,
                    FileSize = request.FileSize ?? 0,
                    ContentType = request.ContentType ?? "application/pdf",
                    UploadedAt = DateTime.UtcNow
                };

                FtpLogger.LogStep(2, "Preparing to upload file to Google Drive", true);
                FtpLogger.LogInfo(string.Format("  FileName: {0}", consentDocument.FileName));
                FtpLogger.LogInfo(string.Format("  RequirementId: {0}", request.RequirementId));
                FtpLogger.LogInfo(string.Format("  BankId: {0}", request.BankId));
                FtpLogger.LogInfo(string.Format("  ContentType: {0}", consentDocument.ContentType));

                LogRequest(string.Format("UploadConsentDocument - Attempting to upload file: {0}", consentDocument.FileName), true);

                // Upload the file to Google Drive
                FtpLogger.LogStep(3, "Calling Google Drive Storage Service", true,
                    string.Format("Uploading: {0} for {1}/{2}", consentDocument.FileName, request.RequirementId, request.BankId));

                var uploadedFileName = _driveStorage.UploadConsentDocument(
                    request.RequirementId,
                    request.BankId,
                    consentDocument);

                if (string.IsNullOrEmpty(uploadedFileName))
                {
                    FtpLogger.LogError(string.Format("UPLOAD FAILED - Storage service returned null/empty fileName [RequestId: {0}]", requestId));
                    LogRequest("UploadConsentDocument - Upload failed", false);
                    return Request.CreateResponse(HttpStatusCode.InternalServerError,
                        new ApiResponse
                        {
                            Success = false,
                            Message = "Failed to upload consent document to Google Drive",
                            Error = "UPLOAD_FAILED",
                            ErrorCode = "STORAGE_ERROR"
                        });
                }

                FtpLogger.LogStep(3, "Google Drive Storage Service completed", true,
                    string.Format("File uploaded successfully: {0}", uploadedFileName));

                LogRequest(string.Format("UploadConsentDocument - File uploaded successfully: {0}", uploadedFileName), true);

                // Prepare success response
                var apiResponse = new ApiResponse
                {
                    Success = true,
                    Message = "Consent document uploaded successfully to Google Drive",
                    Data = new
                    {
                        fileName = uploadedFileName,
                        originalFileName = request.FileName,
                        requirementId = request.RequirementId,
                        bankId = request.BankId,
                        quoteId = request.QuoteId,
                        fileSize = consentDocument.FileSize,
                        contentType = consentDocument.ContentType,
                        uploadedAt = DateTime.UtcNow,
                        uploadedBy = request.UploadedBy,
                        storagePath = string.Format("DepositManager/BankConsent/{0}/{1}/{2}", 
                            request.RequirementId, request.BankId, uploadedFileName),
                        downloadUrl = string.Format("/api/deposits/consent/googledrive/download?requirementId={0}&bankId={1}&fileName={2}",
                            request.RequirementId, request.BankId, uploadedFileName)
                    }
                };

                FtpLogger.LogInfo("=== GOOGLE DRIVE UPLOAD REQUEST COMPLETED SUCCESSFULLY ===");
                FtpLogger.LogInfo(string.Format("RequestId: {0}, FileName: {1}", requestId, uploadedFileName));
                FtpLogger.LogInfo("================================================================================");
                FtpLogger.LogInfo("");

                LogRequest(string.Format("UploadConsentDocument - Success: {0}", uploadedFileName), true);
                return Request.CreateResponse(HttpStatusCode.Created, apiResponse);
            }
            catch (ArgumentNullException argEx)
            {
                FtpLogger.LogError("================================================================================");
                FtpLogger.LogError(string.Format("=== GOOGLE DRIVE UPLOAD REQUEST FAILED (NULL ARGUMENT) - RequestId: {0} ===", requestId));
                FtpLogger.LogError("================================================================================");
                FtpLogger.LogError(string.Format("Parameter: {0}", argEx.ParamName));
                FtpLogger.LogError(string.Format("Message: {0}", argEx.Message));
                FtpLogger.LogError("================================================================================");
                FtpLogger.LogError("");

                LogError("UploadConsentDocument", argEx);

                return Request.CreateResponse(HttpStatusCode.BadRequest,
                    new ApiResponse
                    {
                        Success = false,
                        Message = string.Format("Required parameter is missing: {0}", argEx.ParamName),
                        Error = argEx.Message,
                        ErrorCode = "NULL_PARAMETER"
                    });
            }
            catch (ArgumentException argEx)
            {
                FtpLogger.LogError("================================================================================");
                FtpLogger.LogError(string.Format("=== GOOGLE DRIVE UPLOAD REQUEST FAILED (INVALID ARGUMENT) - RequestId: {0} ===", requestId));
                FtpLogger.LogError("================================================================================");
                FtpLogger.LogError(string.Format("Parameter: {0}", argEx.ParamName));
                FtpLogger.LogError(string.Format("Message: {0}", argEx.Message));
                FtpLogger.LogError("================================================================================");
                FtpLogger.LogError("");

                LogError("UploadConsentDocument", argEx);

                return Request.CreateResponse(HttpStatusCode.BadRequest,
                    new ApiResponse
                    {
                        Success = false,
                        Message = argEx.Message,
                        Error = "INVALID_PARAMETER",
                        ErrorCode = "INVALID_ARGUMENT"
                    });
            }
            catch (InvalidOperationException invEx)
            {
                FtpLogger.LogError("================================================================================");
                FtpLogger.LogError(string.Format("=== GOOGLE DRIVE UPLOAD REQUEST FAILED (INVALID OPERATION) - RequestId: {0} ===", requestId));
                FtpLogger.LogError("================================================================================");
                FtpLogger.LogError(string.Format("Message: {0}", invEx.Message));
                FtpLogger.LogError("================================================================================");
                FtpLogger.LogError("");

                LogError("UploadConsentDocument", invEx);

                return Request.CreateResponse(HttpStatusCode.BadRequest,
                    new ApiResponse
                    {
                        Success = false,
                        Message = invEx.Message,
                        Error = "INVALID_OPERATION",
                        ErrorCode = "OPERATION_FAILED"
                    });
            }
            catch (Exception ex)
            {
                FtpLogger.LogError("================================================================================");
                FtpLogger.LogError(string.Format("=== GOOGLE DRIVE UPLOAD REQUEST FAILED - RequestId: {0} ===", requestId));
                FtpLogger.LogError("================================================================================");
                FtpLogger.LogError(string.Format("Exception Type: {0}", ex.GetType().FullName));
                FtpLogger.LogError(string.Format("Exception Message: {0}", ex.Message));
                FtpLogger.LogError(string.Format("Stack Trace: {0}", ex.StackTrace));
                FtpLogger.LogError("--- Request Context ---");
                FtpLogger.LogError(string.Format("  RequirementId: {0}", request?.RequirementId ?? "(null)"));
                FtpLogger.LogError(string.Format("  BankId: {0}", request?.BankId ?? "(null)"));
                FtpLogger.LogError(string.Format("  FileName: {0}", request?.FileName ?? "(null)"));
                FtpLogger.LogError(string.Format("  Client IP: {0}", clientIp));
                FtpLogger.LogError("================================================================================");
                FtpLogger.LogError("");

                LogError("UploadConsentDocument", ex);
                LogRequest(string.Format("UploadConsentDocument - Exception Details:"), false);
                LogRequest(string.Format("  Type: {0}", ex.GetType().Name), false);
                LogRequest(string.Format("  Message: {0}", ex.Message), false);
                LogRequest(string.Format("  Stack: {0}", ex.StackTrace), false);

                return Request.CreateResponse(HttpStatusCode.InternalServerError,
                    new ApiResponse
                    {
                        Success = false,
                        Message = "An error occurred while uploading the consent document to Google Drive",
                        Error = ex.Message,
                        ErrorCode = "SERVER_ERROR",
                        Data = new
                        {
                            hint = "Check detailed logs at: C:\\smkcapi_published\\Logs\\FtpLog_YYYYMMDD.txt",
                            requirementId = request?.RequirementId,
                            bankId = request?.BankId,
                            fileName = request?.FileName,
                            requestId = requestId
                        }
                    });
            }
        }

        /// <summary>
        /// Download bank consent document from Google Drive by requirement ID and bank ID
        /// GET: /api/deposits/consent/googledrive/download
        /// NO AUTHENTICATION REQUIRED - Plain access
        /// </summary>
        /// <param name="requirementId">Requirement identifier (e.g., REQ0000000001)</param>
        /// <param name="bankId">Bank identifier</param>
        /// <param name="fileName">Consent file name</param>
        /// <returns>Binary file download or JSON response with base64 data</returns>
        [HttpGet]
        [Route("download")]
        [AllowAnonymous]
        public HttpResponseMessage DownloadConsentDocument(string requirementId, string bankId, string fileName)
        {
            var requestId = Guid.NewGuid().ToString();
            var clientIp = GetClientIpAddress();

            try
            {
                // Log request start
                FtpLogger.LogInfo("================================================================================");
                FtpLogger.LogInfo(string.Format("=== GOOGLE DRIVE DOWNLOAD REQUEST START - RequestId: {0} ===", requestId));
                FtpLogger.LogInfo("================================================================================");
                FtpLogger.LogInfo(string.Format("Client IP: {0}", clientIp));
                FtpLogger.LogInfo(string.Format("RequestId: {0}", requestId));
                FtpLogger.LogInfo(string.Format("Timestamp: {0:yyyy-MM-dd HH:mm:ss.fff} UTC", DateTime.UtcNow));
                FtpLogger.LogInfo(string.Format("Authentication: NONE (Plain Access)"));
                FtpLogger.LogInfo("--- Request Parameters ---");
                FtpLogger.LogInfo(string.Format("  RequirementId: {0}", requirementId ?? "(null)"));
                FtpLogger.LogInfo(string.Format("  BankId: {0}", bankId ?? "(null)"));
                FtpLogger.LogInfo(string.Format("  FileName: {0}", fileName ?? "(null)"));

                LogRequest(string.Format("DownloadConsentDocument - Starting request"), true);
                LogRequest(string.Format("  RequirementId: {0}", requirementId ?? "(null)"), true);
                LogRequest(string.Format("  BankId: {0}", bankId ?? "(null)"), true);
                LogRequest(string.Format("  FileName: {0}", fileName ?? "(null)"), true);

                // Validate required parameters
                FtpLogger.LogStep(1, "Validating request parameters", true);
                
                if (string.IsNullOrWhiteSpace(requirementId))
                {
                    FtpLogger.LogError(string.Format("VALIDATION FAILED - Missing requirementId [RequestId: {0}]", requestId));
                    LogRequest("DownloadConsentDocument - Missing requirementId", false);
                    return Request.CreateResponse(HttpStatusCode.BadRequest, 
                        new ApiResponse 
                        { 
                            Success = false, 
                            Message = "requirementId is required",
                            Error = "INVALID_PARAMETER"
                        });
                }

                if (string.IsNullOrWhiteSpace(bankId))
                {
                    FtpLogger.LogError(string.Format("VALIDATION FAILED - Missing bankId [RequestId: {0}]", requestId));
                    LogRequest("DownloadConsentDocument - Missing bankId", false);
                    return Request.CreateResponse(HttpStatusCode.BadRequest, 
                        new ApiResponse 
                        { 
                            Success = false, 
                            Message = "bankId is required",
                            Error = "INVALID_PARAMETER"
                        });
                }

                if (string.IsNullOrWhiteSpace(fileName))
                {
                    FtpLogger.LogError(string.Format("VALIDATION FAILED - Missing fileName [RequestId: {0}]", requestId));
                    LogRequest("DownloadConsentDocument - Missing fileName", false);
                    return Request.CreateResponse(HttpStatusCode.BadRequest, 
                        new ApiResponse 
                        { 
                            Success = false, 
                            Message = "fileName is required",
                            Error = "INVALID_PARAMETER"
                        });
                }

                FtpLogger.LogStep(1, "Validating request parameters", true, "All required parameters present");
                LogRequest(string.Format("DownloadConsentDocument - Parameters validated"), true);

                FtpLogger.LogStep(2, "Preparing to download file from Google Drive", true);
                FtpLogger.LogInfo(string.Format("  Target FileName: {0}", fileName));
                FtpLogger.LogInfo(string.Format("  RequirementId: {0}", requirementId));
                FtpLogger.LogInfo(string.Format("  BankId: {0}", bankId));
                
                LogRequest(string.Format("DownloadConsentDocument - Attempting to download file: {0}", fileName), true);

                // Download the file from Google Drive
                FtpLogger.LogStep(3, "Calling Google Drive Storage Service", true, 
                    string.Format("Downloading: {0} for {1}/{2}", fileName, requirementId, bankId));
                
                var base64Content = _driveStorage.DownloadConsentDocument(fileName, requirementId, bankId);
                
                if (base64Content == null)
                {
                    FtpLogger.LogError(string.Format("FILE NOT FOUND - {0} [RequestId: {1}]", fileName, requestId));
                    FtpLogger.LogError(string.Format("  Expected path: DepositManager/BankConsent/{0}/{1}/{2}", 
                        requirementId, bankId, fileName));
                    
                    LogRequest(string.Format("DownloadConsentDocument - File not found: {0}", fileName), false);
                    LogRequest(string.Format("  Expected location: DepositManager/BankConsent/{0}/{1}/{2}", 
                        requirementId, bankId, fileName), false);
                    return Request.CreateResponse(HttpStatusCode.NotFound, 
                        new ApiResponse 
                        { 
                            Success = false, 
                            Message = "Consent document not found on Google Drive",
                            Error = "FILE_NOT_FOUND",
                            Data = new 
                            {
                                requirementId = requirementId,
                                bankId = bankId,
                                fileName = fileName,
                                hint = "Check logs at: C:\\smkcapi_published\\Logs\\FtpLog_YYYYMMDD.txt for detailed path information"
                            }
                        });
                }

                FtpLogger.LogStep(3, "Google Drive Storage Service completed", true, 
                    string.Format("File retrieved, Base64 size: {0} chars", base64Content.Length));
                
                LogRequest(string.Format("DownloadConsentDocument - File retrieved successfully, size: {0} chars (base64)", base64Content.Length), true);

                // Determine if client wants binary download or JSON response
                var acceptHeader = Request.Headers.Accept?.ToString() ?? string.Empty;
                var returnBinary = acceptHeader.Contains("application/pdf") || 
                                  acceptHeader.Contains("application/octet-stream") ||
                                  acceptHeader.Contains("*/*");

                FtpLogger.LogStep(4, "Determining response format", true);
                FtpLogger.LogInfo(string.Format("  Accept Header: {0}", string.IsNullOrEmpty(acceptHeader) ? "(empty)" : acceptHeader));
                FtpLogger.LogInfo(string.Format("  Response Format: {0}", returnBinary ? "Binary PDF" : "JSON with Base64"));
                
                LogRequest(string.Format("DownloadConsentDocument - Accept header: {0}", acceptHeader), true);
                LogRequest(string.Format("DownloadConsentDocument - Return format: {0}", returnBinary ? "Binary PDF" : "JSON with Base64"), true);

                if (returnBinary)
                {
                    // Return as binary file download
                    var fileBytes = Convert.FromBase64String(base64Content);
                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent(fileBytes)
                    };
                    
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                    response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                    {
                        FileName = fileName
                    };
                    response.Content.Headers.ContentLength = fileBytes.Length;

                    FtpLogger.LogStep(5, "Preparing binary response", true, 
                        string.Format("File size: {0} bytes ({1} KB)", fileBytes.Length, fileBytes.Length / 1024.0));
                    FtpLogger.LogInfo(string.Format("  Content-Type: application/pdf"));
                    FtpLogger.LogInfo(string.Format("  Content-Disposition: attachment; filename=\"{0}\"", fileName));
                    FtpLogger.LogInfo("=== GOOGLE DRIVE DOWNLOAD REQUEST COMPLETED SUCCESSFULLY (BINARY) ===");
                    FtpLogger.LogInfo(string.Format("RequestId: {0}", requestId));
                    FtpLogger.LogInfo("================================================================================");
                    FtpLogger.LogInfo("");

                    LogRequest(string.Format("DownloadConsentDocument - Success (Binary): {0} bytes", fileBytes.Length), true);
                    return response;
                }
                else
                {
                    // Return as JSON with base64 data
                    var apiResponse = new ApiResponse
                    {
                        Success = true,
                        Message = "Consent document retrieved successfully from Google Drive",
                        Data = new
                        {
                            fileName = fileName,
                            fileData = base64Content,
                            contentType = "application/pdf",
                            requirementId = requirementId,
                            bankId = bankId,
                            downloadedAt = DateTime.UtcNow,
                            storageLocation = "Google Drive"
                        }
                    };

                    FtpLogger.LogStep(5, "Preparing JSON response", true, 
                        string.Format("Base64 data size: {0} chars", base64Content.Length));
                    FtpLogger.LogInfo("=== GOOGLE DRIVE DOWNLOAD REQUEST COMPLETED SUCCESSFULLY (JSON) ===");
                    FtpLogger.LogInfo(string.Format("RequestId: {0}", requestId));
                    FtpLogger.LogInfo("================================================================================");
                    FtpLogger.LogInfo("");

                    LogRequest(string.Format("DownloadConsentDocument - Success (JSON): {0}", fileName), true);
                    return Request.CreateResponse(HttpStatusCode.OK, apiResponse);
                }
            }
            catch (Exception ex)
            {
                FtpLogger.LogError("================================================================================");
                FtpLogger.LogError(string.Format("=== GOOGLE DRIVE DOWNLOAD REQUEST FAILED - RequestId: {0} ===", requestId));
                FtpLogger.LogError("================================================================================");
                FtpLogger.LogError(string.Format("Exception Type: {0}", ex.GetType().FullName));
                FtpLogger.LogError(string.Format("Exception Message: {0}", ex.Message));
                FtpLogger.LogError(string.Format("Stack Trace: {0}", ex.StackTrace));
                FtpLogger.LogError("--- Request Context ---");
                FtpLogger.LogError(string.Format("  RequirementId: {0}", requirementId ?? "(null)"));
                FtpLogger.LogError(string.Format("  BankId: {0}", bankId ?? "(null)"));
                FtpLogger.LogError(string.Format("  FileName: {0}", fileName ?? "(null)"));
                FtpLogger.LogError(string.Format("  Client IP: {0}", clientIp));
                FtpLogger.LogError("================================================================================");
                FtpLogger.LogError("");
                
                LogError("DownloadConsentDocument", ex);
                LogRequest(string.Format("DownloadConsentDocument - Exception Details:"), false);
                LogRequest(string.Format("  Type: {0}", ex.GetType().Name), false);
                LogRequest(string.Format("  Message: {0}", ex.Message), false);
                LogRequest(string.Format("  Stack: {0}", ex.StackTrace), false);
                
                return Request.CreateResponse(HttpStatusCode.InternalServerError, 
                    new ApiResponse 
                    { 
                        Success = false, 
                        Message = "An error occurred while downloading the consent document from Google Drive",
                        Error = ex.Message,
                        Data = new
                        {
                            hint = "Check detailed logs at: C:\\smkcapi_published\\Logs\\FtpLog_YYYYMMDD.txt",
                            requirementId = requirementId,
                            bankId = bankId,
                            fileName = fileName,
                            requestId = requestId
                        }
                    });
            }
        }

        /// <summary>
        /// Get consent document information without downloading
        /// GET: /api/deposits/consent/googledrive/info
        /// NO AUTHENTICATION REQUIRED - Plain access
        /// </summary>
        /// <param name="requirementId">Requirement identifier</param>
        /// <param name="bankId">Bank identifier</param>
        /// <param name="fileName">Consent file name</param>
        /// <returns>Document metadata</returns>
        [HttpGet]
        [Route("info")]
        [AllowAnonymous]
        public HttpResponseMessage GetConsentDocumentInfo(string requirementId, string bankId, string fileName)
        {
            var requestId = Guid.NewGuid().ToString();
            var clientIp = GetClientIpAddress();

            try
            {
                // Log request start
                FtpLogger.LogInfo("================================================================================");
                FtpLogger.LogInfo(string.Format("=== GOOGLE DRIVE INFO REQUEST START - RequestId: {0} ===", requestId));
                FtpLogger.LogInfo("================================================================================");
                FtpLogger.LogInfo(string.Format("Client IP: {0}", clientIp));
                FtpLogger.LogInfo(string.Format("RequestId: {0}", requestId));
                FtpLogger.LogInfo(string.Format("Timestamp: {0:yyyy-MM-dd HH:mm:ss.fff} UTC", DateTime.UtcNow));
                FtpLogger.LogInfo(string.Format("Authentication: NONE (Plain Access)"));
                FtpLogger.LogInfo("--- Request Parameters ---");
                FtpLogger.LogInfo(string.Format("  RequirementId: {0}", requirementId ?? "(null)"));
                FtpLogger.LogInfo(string.Format("  BankId: {0}", bankId ?? "(null)"));
                FtpLogger.LogInfo(string.Format("  FileName: {0}", fileName ?? "(null)"));

                LogRequest(string.Format("GetConsentDocumentInfo - Starting request"), true);
                LogRequest(string.Format("  RequirementId: {0}", requirementId ?? "(null)"), true);
                LogRequest(string.Format("  BankId: {0}", bankId ?? "(null)"), true);
                LogRequest(string.Format("  FileName: {0}", fileName ?? "(null)"), true);

                // Validate required parameters
                FtpLogger.LogStep(1, "Validating request parameters", true);
                
                if (string.IsNullOrWhiteSpace(requirementId) || 
                    string.IsNullOrWhiteSpace(bankId) || 
                    string.IsNullOrWhiteSpace(fileName))
                {
                    FtpLogger.LogError(string.Format("VALIDATION FAILED - Missing required parameters [RequestId: {0}]", requestId));
                    FtpLogger.LogError(string.Format("  RequirementId: {0}", string.IsNullOrWhiteSpace(requirementId) ? "MISSING" : "OK"));
                    FtpLogger.LogError(string.Format("  BankId: {0}", string.IsNullOrWhiteSpace(bankId) ? "MISSING" : "OK"));
                    FtpLogger.LogError(string.Format("  FileName: {0}", string.IsNullOrWhiteSpace(fileName) ? "MISSING" : "OK"));
                    
                    LogRequest("GetConsentDocumentInfo - Missing parameters", false);
                    return Request.CreateResponse(HttpStatusCode.BadRequest, 
                        new ApiResponse 
                        { 
                            Success = false, 
                            Message = "requirementId, bankId, and fileName are required",
                            Error = "INVALID_PARAMETER"
                        });
                }

                FtpLogger.LogStep(1, "Validating request parameters", true, "All required parameters present");
                LogRequest(string.Format("GetConsentDocumentInfo - Parameters validated"), true);
                
                FtpLogger.LogStep(2, "Checking file existence on Google Drive", true, 
                    string.Format("File: {0}, Requirement: {1}, Bank: {2}", fileName, requirementId, bankId));
                LogRequest(string.Format("GetConsentDocumentInfo - Checking file existence: {0}", fileName), true);

                // Check if file exists by attempting to download metadata
                var base64Content = _driveStorage.DownloadConsentDocument(fileName, requirementId, bankId);
                
                if (base64Content == null)
                {
                    FtpLogger.LogError(string.Format("FILE NOT FOUND - {0} [RequestId: {1}]", fileName, requestId));
                    FtpLogger.LogError(string.Format("  Expected path: DepositManager/BankConsent/{0}/{1}/{2}", 
                        requirementId, bankId, fileName));
                    FtpLogger.LogInfo("=== GOOGLE DRIVE INFO REQUEST COMPLETED (FILE NOT FOUND) ===");
                    FtpLogger.LogInfo(string.Format("RequestId: {0}", requestId));
                    FtpLogger.LogInfo("================================================================================");
                    FtpLogger.LogInfo("");
                    
                    LogRequest(string.Format("GetConsentDocumentInfo - File not found: {0}", fileName), false);
                    LogRequest(string.Format("  Expected path: DepositManager/BankConsent/{0}/{1}/{2}", requirementId, bankId, fileName), false);
                    return Request.CreateResponse(HttpStatusCode.NotFound, 
                        new ApiResponse 
                        { 
                            Success = false, 
                            Message = "Consent document not found on Google Drive",
                            Error = "FILE_NOT_FOUND",
                            Data = new
                            {
                                requirementId = requirementId,
                                bankId = bankId,
                                fileName = fileName,
                                hint = "Check logs at: C:\\smkcapi_published\\Logs\\FtpLog_YYYYMMDD.txt for detailed path information"
                            }
                        });
                }

                var fileBytes = Convert.FromBase64String(base64Content);
                
                FtpLogger.LogStep(3, "File information retrieved", true, 
                    string.Format("File size: {0} bytes ({1:N2} KB)", fileBytes.Length, fileBytes.Length / 1024.0));
                FtpLogger.LogInfo(string.Format("  Content-Type: application/pdf"));
                FtpLogger.LogInfo(string.Format("  File exists: true"));
                
                LogRequest(string.Format("GetConsentDocumentInfo - File found, size: {0} bytes", fileBytes.Length), true);

                var response = new ApiResponse
                {
                    Success = true,
                    Message = "Consent document information retrieved successfully from Google Drive",
                    Data = new
                    {
                        fileName = fileName,
                        fileSize = fileBytes.Length,
                        contentType = "application/pdf",
                        requirementId = requirementId,
                        bankId = bankId,
                        exists = true,
                        storageLocation = "Google Drive",
                        storagePath = string.Format("DepositManager/BankConsent/{0}/{1}/{2}", requirementId, bankId, fileName),
                        downloadUrl = string.Format("/api/deposits/consent/googledrive/download?requirementId={0}&bankId={1}&fileName={2}", 
                            requirementId, bankId, fileName)
                    }
                };

                FtpLogger.LogInfo("=== GOOGLE DRIVE INFO REQUEST COMPLETED SUCCESSFULLY ===");
                FtpLogger.LogInfo(string.Format("RequestId: {0}, FileSize: {1} bytes", requestId, fileBytes.Length));
                FtpLogger.LogInfo("================================================================================");
                FtpLogger.LogInfo("");

                LogRequest(string.Format("GetConsentDocumentInfo - Success: {0}", fileName), true);
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception ex)
            {
                FtpLogger.LogError("================================================================================");
                FtpLogger.LogError(string.Format("=== GOOGLE DRIVE INFO REQUEST FAILED - RequestId: {0} ===", requestId));
                FtpLogger.LogError("================================================================================");
                FtpLogger.LogError(string.Format("Exception Type: {0}", ex.GetType().FullName));
                FtpLogger.LogError(string.Format("Exception Message: {0}", ex.Message));
                FtpLogger.LogError(string.Format("Stack Trace: {0}", ex.StackTrace));
                FtpLogger.LogError("--- Request Context ---");
                FtpLogger.LogError(string.Format("  RequirementId: {0}", requirementId ?? "(null)"));
                FtpLogger.LogError(string.Format("  BankId: {0}", bankId ?? "(null)"));
                FtpLogger.LogError(string.Format("  FileName: {0}", fileName ?? "(null)"));
                FtpLogger.LogError(string.Format("  Client IP: {0}", clientIp));
                FtpLogger.LogError("================================================================================");
                FtpLogger.LogError("");
                
                LogError("GetConsentDocumentInfo", ex);
                LogRequest(string.Format("GetConsentDocumentInfo - Exception Details:"), false);
                LogRequest(string.Format("  Type: {0}", ex.GetType().Name), false);
                LogRequest(string.Format("  Message: {0}", ex.Message), false);
                
                return Request.CreateResponse(HttpStatusCode.InternalServerError, 
                    new ApiResponse 
                    { 
                        Success = false, 
                        Message = "An error occurred while retrieving consent document information from Google Drive",
                        Error = ex.Message,
                        Data = new
                        {
                            hint = "Check detailed logs at: C:\\smkcapi_published\\Logs\\FtpLog_YYYYMMDD.txt",
                            requirementId = requirementId,
                            bankId = bankId,
                            fileName = fileName,
                            requestId = requestId
                        }
                    });
            }
        }

        /// <summary>
        /// Get client IP address from request
        /// </summary>
        private string GetClientIpAddress()
        {
            try
            {
                if (Request.Properties.ContainsKey("MS_HttpContext"))
                {
                    var httpContext = Request.Properties["MS_HttpContext"] as System.Web.HttpContextWrapper;
                    if (httpContext != null)
                    {
                        var ipAddress = httpContext.Request.UserHostAddress;
                        return ipAddress ?? "Unknown";
                    }
                }
                return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private void LogRequest(string action, bool success)
        {
            var requestId = "Unknown";
            var clientIp = GetClientIpAddress();

            var logEntry = string.Format("{0:yyyy-MM-dd HH:mm:ss} UTC - GOOGLE_DRIVE_CONSENT_CONTROLLER_{1} - Status: {2}, ClientIP: {3}, RequestId: {4}",
                DateTime.UtcNow,
                action.ToUpper(),
                success ? "SUCCESS" : "FAILED",
                clientIp,
                requestId);
            
            System.Diagnostics.Trace.TraceInformation(logEntry);
        }

        private void LogError(string action, Exception ex)
        {
            var logEntry = string.Format("{0:yyyy-MM-dd HH:mm:ss} UTC - GOOGLE_DRIVE_CONSENT_CONTROLLER_{1}_ERROR: {2}",
                DateTime.UtcNow,
                action.ToUpper(),
                ex.Message);
            System.Diagnostics.Trace.TraceError(logEntry);

            var clientIp = GetClientIpAddress();
            var securityLogEntry = string.Format("{0:yyyy-MM-dd HH:mm:ss} UTC - SECURITY_ERROR - Controller: GoogleDriveConsentController, Action: {1}, ClientIP: {2}, Error: {3}",
                DateTime.UtcNow,
                action,
                clientIp,
                ex.Message);
            System.Diagnostics.Trace.TraceError(securityLogEntry);
        }
    }
}
