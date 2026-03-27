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
    /// Citizen Portal API Controller
    /// Handles citizen registration, OTP verification, and booking operations
    /// </summary>
    [RoutePrefix("api/citizen")]
    [ShaAuthentication]
    [RateLimit(maxRequests: 100, timeWindowMinutes: 1)]
    public class CitizenController : ApiController
    {
        private readonly IParkBookingService _parkBookingService;

        public CitizenController(IParkBookingService parkBookingService)
        {
            _parkBookingService = parkBookingService ?? throw new ArgumentNullException(nameof(parkBookingService));
        }

        /// <summary>
        /// Register a new citizen and send OTP to mobile
        /// POST: /api/citizen/register
        /// </summary>
        [HttpPost]
        [Route("register")]
        public async Task<IHttpActionResult> Register([FromBody] CitizenRegistrationRequest request)
        {
            try
            {
                if (request == null)
                {
                    return Content(HttpStatusCode.BadRequest, 
                        ApiResponse<object>.CreateError("Request body is required", "MISSING_REQUEST_BODY"));
                }

                var result = await _parkBookingService.RegisterCitizenAsync(request);

                if (!result.Success)
                {
                    return Content(HttpStatusCode.BadRequest, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                LogError("Register", ex);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Verify OTP sent to citizen's mobile
        /// POST: /api/citizen/verify-otp
        /// </summary>
        [HttpPost]
        [Route("verify-otp")]
        public async Task<IHttpActionResult> VerifyOtp([FromBody] OTPVerificationRequest request)
        {
            try
            {
                if (request == null)
                {
                    return Content(HttpStatusCode.BadRequest,
                        ApiResponse<object>.CreateError("Request body is required", "MISSING_REQUEST_BODY"));
                }

                var result = await _parkBookingService.VerifyOtpAsync(request);

                if (!result.Success)
                {
                    return Content(HttpStatusCode.Unauthorized, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                LogError("VerifyOtp", ex);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Resend OTP to citizen's mobile
        /// POST: /api/citizen/resend-otp
        /// </summary>
        [HttpPost]
        [Route("resend-otp")]
        public async Task<IHttpActionResult> ResendOtp([FromBody] ResendOTPRequest request)
        {
            try
            {
                if (request == null)
                {
                    return Content(HttpStatusCode.BadRequest,
                        ApiResponse<object>.CreateError("Request body is required", "MISSING_REQUEST_BODY"));
                }

                var result = await _parkBookingService.ResendOtpAsync(request);

                if (!result.Success)
                {
                    return Content(HttpStatusCode.BadRequest, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                LogError("ResendOtp", ex);
                return InternalServerError();
            }
        }

        private void LogError(string action, Exception ex)
        {
            var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - CITIZEN_CONTROLLER_{action.ToUpper()}_ERROR: {ex.Message}";
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
