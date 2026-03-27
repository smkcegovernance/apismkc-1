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
    /// Department Portal API Controller
    /// Handles department user authentication and admin operations
    /// </summary>
    [RoutePrefix("api/department")]
    [ShaAuthentication]
    [RateLimit(maxRequests: 100, timeWindowMinutes: 1)]
    public class DepartmentController : ApiController
    {
        private readonly IParkBookingService _parkBookingService;

        public DepartmentController(IParkBookingService parkBookingService)
        {
            _parkBookingService = parkBookingService ?? throw new ArgumentNullException(nameof(parkBookingService));
        }

        /// <summary>
        /// Department user login
        /// POST: /api/department/login
        /// </summary>
        [HttpPost]
        [Route("login")]
        public async Task<IHttpActionResult> Login([FromBody] DepartmentLoginRequest request)
        {
            try
            {
                if (request == null)
                {
                    return Content(HttpStatusCode.BadRequest,
                        ApiResponse<object>.CreateError("Request body is required", "MISSING_REQUEST_BODY"));
                }

                var result = await _parkBookingService.DepartmentLoginAsync(request);

                if (!result.Success)
                {
                    return Content(HttpStatusCode.Unauthorized, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                LogError("Login", ex);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Get dashboard statistics
        /// GET: /api/department/dashboard/stats
        /// </summary>
        [HttpGet]
        [Route("dashboard/stats")]
        public async Task<IHttpActionResult> GetDashboardStats()
        {
            try
            {
                var userId = User?.Identity?.Name ?? "unknown";

                var result = await _parkBookingService.GetDashboardStatsAsync(userId);

                if (!result.Success)
                {
                    return Content(HttpStatusCode.BadRequest, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                LogError("GetDashboardStats", ex);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Get all bookings with pagination and filters
        /// GET: /api/department/bookings?page=1&pageSize=10&status=CONFIRMED
        /// </summary>
        [HttpGet]
        [Route("bookings")]
        public async Task<IHttpActionResult> GetAllBookings([FromUri] BookingFilterRequest request)
        {
            try
            {
                if (request == null)
                {
                    request = new BookingFilterRequest();
                }

                var result = await _parkBookingService.GetAllBookingsAsync(request);

                if (!result.Success)
                {
                    return Content(HttpStatusCode.BadRequest, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                LogError("GetAllBookings", ex);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Search booking by token
        /// GET: /api/department/bookings/search/{token}
        /// </summary>
        [HttpGet]
        [Route("bookings/search/{token}")]
        public async Task<IHttpActionResult> SearchBookingByToken(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return Content(HttpStatusCode.BadRequest,
                        ApiResponse<object>.CreateError("Token is required", "MISSING_TOKEN"));
                }

                var result = await _parkBookingService.SearchBookingByTokenAsync(token);

                if (!result.Success)
                {
                    return NotFound();
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                LogError("SearchBookingByToken", ex);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Verify citizen entry at park
        /// POST: /api/department/bookings/{bookingId}/verify
        /// </summary>
        [HttpPost]
        [Route("bookings/{bookingId}/verify")]
        public async Task<IHttpActionResult> VerifyEntry(string bookingId, [FromBody] VerifyEntryRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(bookingId))
                {
                    return Content(HttpStatusCode.BadRequest,
                        ApiResponse<object>.CreateError("Booking ID is required", "MISSING_BOOKING_ID"));
                }

                if (request == null)
                {
                    return Content(HttpStatusCode.BadRequest,
                        ApiResponse<object>.CreateError("Request body is required", "MISSING_REQUEST_BODY"));
                }

                var result = await _parkBookingService.VerifyEntryAsync(bookingId, request);

                if (!result.Success)
                {
                    return Content(HttpStatusCode.BadRequest, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                LogError("VerifyEntry", ex);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Cancel a booking (admin only)
        /// POST: /api/department/bookings/{bookingId}/cancel
        /// </summary>
        [HttpPost]
        [Route("bookings/{bookingId}/cancel")]
        public async Task<IHttpActionResult> CancelBooking(string bookingId, [FromBody] CancelBookingRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(bookingId))
                {
                    return Content(HttpStatusCode.BadRequest,
                        ApiResponse<object>.CreateError("Booking ID is required", "MISSING_BOOKING_ID"));
                }

                if (request == null)
                {
                    return Content(HttpStatusCode.BadRequest,
                        ApiResponse<object>.CreateError("Request body is required", "MISSING_REQUEST_BODY"));
                }

                var result = await _parkBookingService.CancelBookingAsync(bookingId, request);

                if (!result.Success)
                {
                    return Content(HttpStatusCode.BadRequest, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                LogError("CancelBooking", ex);
                return InternalServerError();
            }
        }

        private void LogError(string action, Exception ex)
        {
            var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - DEPARTMENT_CONTROLLER_{action.ToUpper()}_ERROR: {ex.Message}";
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
