using System;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using SmkcApi.Models;
using SmkcApi.Security;
using SmkcApi.Services;

namespace SmkcApi.Controllers
{
    /// <summary>
    /// Voters API Controller
    /// Handles duplicate voter management operations
    /// </summary>
    [RoutePrefix("api/voters")]
    [ShaAuthentication]
    [RateLimit(maxRequests: 100, timeWindowMinutes: 1)]
    public class VotersController : ApiController
    {
        private readonly IVoterService _voterService;

        public VotersController(IVoterService voterService)
        {
            _voterService = voterService ?? throw new ArgumentNullException(nameof(voterService));
        }

        /// <summary>
        /// Find potential duplicate voter records
        /// POST: /api/voters/find-duplicates
        /// </summary>
        /// <param name="request">Search criteria with first name, middle name, or last name</param>
        /// <returns>List of potential duplicate voters (unverified records only)</returns>
        [HttpPost]
        [Route("find-duplicates")]
        public async Task<IHttpActionResult> FindDuplicates([FromBody] FindDuplicatesRequest request)
        {
            try
            {
                if (request == null)
                {
                    return Content(HttpStatusCode.BadRequest,
                        ApiResponse<object>.CreateError("Request body is required", "MISSING_REQUEST_BODY"));
                }

                var result = await _voterService.FindDuplicateVotersAsync(request);

                if (!result.Success)
                {
                    return Content(HttpStatusCode.BadRequest, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                LogError("FindDuplicates", ex);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Mark voters as duplicates or not duplicates
        /// POST: /api/voters/mark-duplicates
        /// </summary>
        /// <param name="request">Array of SR_NO values and duplicate flag</param>
        /// <returns>Status and duplication ID (if marking as duplicate)</returns>
        [HttpPost]
        [Route("mark-duplicates")]
        public async Task<IHttpActionResult> MarkDuplicates([FromBody] MarkDuplicatesRequest request)
        {
            try
            {
                if (request == null)
                {
                    return Content(HttpStatusCode.BadRequest,
                        ApiResponse<object>.CreateError("Request body is required", "MISSING_REQUEST_BODY"));
                }

                var result = await _voterService.MarkDuplicatesAsync(request);

                if (!result.Success)
                {
                    return Content(HttpStatusCode.BadRequest, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                LogError("MarkDuplicates", ex);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Get overall verification status and statistics
        /// GET: /api/voters/verification-status
        /// </summary>
        /// <returns>Verification statistics including total, verified, unverified counts and percentage</returns>
        [HttpGet]
        [Route("verification-status")]
        public async Task<IHttpActionResult> GetVerificationStatus()
        {
            try
            {
                var result = await _voterService.GetVerificationStatusAsync();

                if (!result.Success)
                {
                    return Content(HttpStatusCode.BadRequest, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                LogError("GetVerificationStatus", ex);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Get all duplicate groups with their records
        /// GET: /api/voters/duplicate-groups?duplicationId={id}
        /// </summary>
        /// <param name="duplicationId">Optional: Filter by specific duplication group ID</param>
        /// <returns>List of duplicate groups with associated voter records</returns>
        [HttpGet]
        [Route("duplicate-groups")]
        public async Task<IHttpActionResult> GetDuplicateGroups([FromUri] int? duplicationId = null)
        {
            try
            {
                var result = await _voterService.GetDuplicateGroupsAsync(duplicationId);

                if (!result.Success)
                {
                    return Content(HttpStatusCode.BadRequest, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                LogError("GetDuplicateGroups", ex);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Get voter record by SR_NO
        /// GET: /api/voters/{srNo}
        /// </summary>
        /// <param name="srNo">Voter serial number</param>
        /// <returns>Single voter record with all details</returns>
        [HttpGet]
        [Route("{srNo:int}")]
        public async Task<IHttpActionResult> GetVoterBySrNo(int srNo)
        {
            try
            {
                if (srNo <= 0)
                {
                    return Content(HttpStatusCode.BadRequest,
                        ApiResponse<object>.CreateError("SR_NO must be a positive integer", "INVALID_SR_NO"));
                }

                var result = await _voterService.GetVoterBySrNoAsync(srNo);

                if (!result.Success)
                {
                    var statusCode = result.ErrorCode == "VOTER_NOT_FOUND" 
                        ? HttpStatusCode.NotFound 
                        : HttpStatusCode.BadRequest;
                    return Content(statusCode, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                LogError("GetVoterBySrNo", ex);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Reset all verification data (for testing/admin purposes)
        /// POST: /api/voters/reset-verification
        /// </summary>
        /// <returns>Status and count of records/groups reset</returns>
        [HttpPost]
        [Route("reset-verification")]
        public async Task<IHttpActionResult> ResetVerification()
        {
            try
            {
                var result = await _voterService.ResetVerificationAsync();

                if (!result.Success)
                {
                    return Content(HttpStatusCode.BadRequest, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                LogError("ResetVerification", ex);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Get count of unverified records
        /// GET: /api/voters/unverified-count
        /// </summary>
        /// <returns>Count of voters not yet verified</returns>
        [HttpGet]
        [Route("unverified-count")]
        public async Task<IHttpActionResult> GetUnverifiedCount()
        {
            try
            {
                var result = await _voterService.GetUnverifiedCountAsync();

                if (!result.Success)
                {
                    return Content(HttpStatusCode.BadRequest, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                LogError("GetUnverifiedCount", ex);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Voter report with rich filters and pagination
        /// POST: /api/voters/report
        /// </summary>
        [HttpPost]
        [Route("report")]
        public async Task<IHttpActionResult> GetVoterReport([FromBody] VoterReportRequest request)
        {
            try
            {
                if (request == null)
                {
                    return Content(HttpStatusCode.BadRequest,
                        VoterReportResponse.CreateError("Request body is required", "MISSING_REQUEST_BODY"));
                }

                var result = await _voterService.GetVoterReportAsync(request);

                if (!result.Success)
                {
                    return Content(HttpStatusCode.BadRequest, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                LogError("GetVoterReport", ex);
                return InternalServerError();
            }
        }

        private void LogError(string action, Exception ex)
        {
            var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - VOTERS_CONTROLLER_{action.ToUpper()}_ERROR: {ex.Message}";
            System.Diagnostics.Trace.TraceError(logEntry);

            if (Request.Properties.ContainsKey("ApiKey"))
            {
                var apiKey = Request.Properties["ApiKey"].ToString();
                var maskedApiKey = SecurityHelper.MaskSensitiveData(apiKey);
                var requestId = Request.Properties.ContainsKey("RequestId") ? Request.Properties["RequestId"].ToString() : "Unknown";

                var securityLogEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - SECURITY_ERROR - Action: {action}, " +
                                      $"ApiKey: {maskedApiKey}, RequestId: {requestId}, Error: {ex.Message}";
                System.Diagnostics.Trace.TraceError(securityLogEntry);
            }
        }
    }
}
