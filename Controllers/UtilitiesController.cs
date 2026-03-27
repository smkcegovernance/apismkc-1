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
    /// Utilities API Controller
    /// Handles common utility operations like SMS, QR codes, and reports
    /// </summary>
    [RoutePrefix("api/utils")]
    [ShaAuthentication]
    [RateLimit(maxRequests: 100, timeWindowMinutes: 1)]
    public class UtilitiesController : ApiController
    {
        private readonly IParkBookingService _parkBookingService;

        public UtilitiesController(IParkBookingService parkBookingService)
        {
            _parkBookingService = parkBookingService ?? throw new ArgumentNullException(nameof(parkBookingService));
        }

        /// <summary>
        /// Send SMS notification (internal service)
        /// POST: /api/utils/send-sms
        /// </summary>
        [HttpPost]
        [Route("send-sms")]
        public async Task<IHttpActionResult> SendSms([FromBody] SmsRequest request)
        {
            try
            {
                if (request == null)
                {
                    return Content(HttpStatusCode.BadRequest,
                        ApiResponse<object>.CreateError("Request body is required", "MISSING_REQUEST_BODY"));
                }

                var result = await _parkBookingService.SendSmsAsync(request);

                if (!result.Success)
                {
                    return Content(HttpStatusCode.BadRequest, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                LogError("SendSms", ex);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Generate QR code for booking token
        /// POST: /api/utils/generate-qr
        /// </summary>
        [HttpPost]
        [Route("generate-qr")]
        public async Task<IHttpActionResult> GenerateQrCode([FromBody] QrCodeRequest request)
        {
            try
            {
                if (request == null)
                {
                    return Content(HttpStatusCode.BadRequest,
                        ApiResponse<object>.CreateError("Request body is required", "MISSING_REQUEST_BODY"));
                }

                var result = await _parkBookingService.GenerateQrCodeAsync(request);

                if (!result.Success)
                {
                    return Content(HttpStatusCode.BadRequest, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                LogError("GenerateQrCode", ex);
                return InternalServerError();
            }
        }

        private void LogError(string action, Exception ex)
        {
            var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - UTILITIES_CONTROLLER_{action.ToUpper()}_ERROR: {ex.Message}";
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

    /// <summary>
    /// Reports API Controller
    /// Handles booking report generation
    /// </summary>
    [RoutePrefix("api/reports")]
    [ShaAuthentication]
    [RateLimit(maxRequests: 50, timeWindowMinutes: 1)]
    public class ReportsController : ApiController
    {
        private readonly IParkBookingService _parkBookingService;

        public ReportsController(IParkBookingService parkBookingService)
        {
            _parkBookingService = parkBookingService ?? throw new ArgumentNullException(nameof(parkBookingService));
        }

        /// <summary>
        /// Generate booking report with filters
        /// GET: /api/reports/bookings?startDate=2025-11-01&endDate=2025-11-15&format=pdf&reportType=DAILY
        /// </summary>
        [HttpGet]
        [Route("bookings")]
        public async Task<IHttpActionResult> GenerateBookingReport([FromUri] ReportRequest request)
        {
            try
            {
                if (request == null)
                {
                    request = new ReportRequest();
                }

                var result = await _parkBookingService.GenerateBookingReportAsync(request);

                if (!result.Success)
                {
                    return Content(HttpStatusCode.BadRequest, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                LogError("GenerateBookingReport", ex);
                return InternalServerError();
            }
        }

        private void LogError(string action, Exception ex)
        {
            var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - REPORTS_CONTROLLER_{action.ToUpper()}_ERROR: {ex.Message}";
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
