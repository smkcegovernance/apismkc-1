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
    /// Bookings API Controller
    /// Handles booking creation, retrieval, and management
    /// </summary>
    [RoutePrefix("api/bookings")]
    [ShaAuthentication]
    [RateLimit(maxRequests: 100, timeWindowMinutes: 1)]
    public class BookingsController : ApiController
    {
        private readonly IParkBookingService _parkBookingService;

        public BookingsController(IParkBookingService parkBookingService)
        {
            _parkBookingService = parkBookingService ?? throw new ArgumentNullException(nameof(parkBookingService));
        }

        /// <summary>
        /// Create a new park slot booking
        /// POST: /api/bookings/create
        /// </summary>
        [HttpPost]
        [Route("create")]
        public async Task<IHttpActionResult> CreateBooking([FromBody] BookingRequest request)
        {
            try
            {
                if (request == null)
                {
                    return Content(HttpStatusCode.BadRequest,
                        ApiResponse<object>.CreateError("Request body is required", "MISSING_REQUEST_BODY"));
                }

                var result = await _parkBookingService.CreateBookingAsync(request);

                if (!result.Success)
                {
                    var statusCode = result.ErrorCode == "SLOT_FULL" ? HttpStatusCode.Conflict : HttpStatusCode.BadRequest;
                    return Content(statusCode, result);
                }

                return Content(HttpStatusCode.Created, result);
            }
            catch (Exception ex)
            {
                LogError("CreateBooking", ex);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Get booking details by booking ID
        /// GET: /api/bookings/{bookingId}
        /// </summary>
        [HttpGet]
        [Route("{bookingId}")]
        public async Task<IHttpActionResult> GetBookingDetails(string bookingId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(bookingId))
                {
                    return Content(HttpStatusCode.BadRequest,
                        ApiResponse<object>.CreateError("Booking ID is required", "MISSING_BOOKING_ID"));
                }

                var result = await _parkBookingService.GetBookingDetailsAsync(bookingId);

                if (!result.Success)
                {
                    return NotFound();
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                LogError("GetBookingDetails", ex);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Download booking receipt
        /// GET: /api/bookings/{bookingId}/receipt
        /// </summary>
        [HttpGet]
        [Route("{bookingId}/receipt")]
        public async Task<IHttpActionResult> DownloadReceipt(string bookingId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(bookingId))
                {
                    return Content(HttpStatusCode.BadRequest,
                        ApiResponse<object>.CreateError("Booking ID is required", "MISSING_BOOKING_ID"));
                }

                var result = await _parkBookingService.GenerateReceiptAsync(bookingId);

                if (!result.Success)
                {
                    return NotFound();
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                LogError("DownloadReceipt", ex);
                return InternalServerError();
            }
        }

        private void LogError(string action, Exception ex)
        {
            var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - BOOKINGS_CONTROLLER_{action.ToUpper()}_ERROR: {ex.Message}";
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
