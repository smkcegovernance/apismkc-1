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
    /// Slots API Controller
    /// Handles available slots and slot management
    /// </summary>
    [RoutePrefix("api/slots")]
    [ShaAuthentication]
    [RateLimit(maxRequests: 100, timeWindowMinutes: 1)]
    public class SlotsController : ApiController
    {
        private readonly IParkBookingService _parkBookingService;

        public SlotsController(IParkBookingService parkBookingService)
        {
            _parkBookingService = parkBookingService ?? throw new ArgumentNullException(nameof(parkBookingService));
        }

        /// <summary>
        /// Get available time slots for a specific date
        /// GET: /api/slots/available?date=2025-11-15
        /// </summary>
        [HttpGet]
        [Route("available")]
        public async Task<IHttpActionResult> GetAvailableSlots([FromUri] string date)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(date))
                {
                    return Content(HttpStatusCode.BadRequest,
                        ApiResponse<object>.CreateError("Date parameter is required in format YYYY-MM-DD", "MISSING_DATE_PARAMETER"));
                }

                var result = await _parkBookingService.GetAvailableSlotsAsync(date);

                if (!result.Success)
                {
                    return Content(HttpStatusCode.BadRequest, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                LogError("GetAvailableSlots", ex);
                return InternalServerError();
            }
        }

        private void LogError(string action, Exception ex)
        {
            var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - SLOTS_CONTROLLER_{action.ToUpper()}_ERROR: {ex.Message}";
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
