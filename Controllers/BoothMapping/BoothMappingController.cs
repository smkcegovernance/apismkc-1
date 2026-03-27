using System.Net;
using System.Net.Http;
using System.Web.Http;
using SmkcApi.Models.BoothMapping;
using SmkcApi.Services.BoothMapping;

namespace SmkcApi.Controllers.BoothMapping
{
    /// <summary>
    /// Booth Mapping controller for booth data operations
    /// Uses WEBSITE schema (ws/ws) for booth management
    /// </summary>
    [RoutePrefix("api/booth")]
    public class BoothMappingController : ApiController
    {
        private readonly IBoothMappingService _boothService;

        public BoothMappingController(IBoothMappingService boothService)
        {
            _boothService = boothService;
        }

        /// <summary>
        /// Get booth mapping statistics
        /// </summary>
        /// <remarks>
        /// Returns overall booth statistics and optionally user-specific statistics.
        /// Requires authentication token in header.
        /// </remarks>
        /// <param name="userId">Optional 8-character user ID for user-specific stats</param>
        /// <returns>Statistics including total, mapped, and unmapped booth counts</returns>
        /// <response code="200">Statistics retrieved successfully</response>
        /// <response code="401">Unauthorized - missing or invalid token</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [Route("statistics")]
        public HttpResponseMessage GetStatistics([FromUri] string userId = null)
        {
            var result = _boothService.GetStatistics(userId);
            var statusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.InternalServerError;
            return Request.CreateResponse(statusCode, result);
        }

        /// <summary>
        /// Get all booths
        /// </summary>
        /// <remarks>
        /// Returns complete list of all booths with their mapping status.
        /// Requires authentication token in header.
        /// </remarks>
        /// <returns>List of all booths</returns>
        /// <response code="200">Booths retrieved successfully</response>
        /// <response code="401">Unauthorized - missing or invalid token</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [Route("booths")]
        public HttpResponseMessage GetAllBooths()
        {
            var result = _boothService.GetAllBooths();
            var statusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.InternalServerError;
            return Request.CreateResponse(statusCode, result);
        }

        /// <summary>
        /// Search booths with filters
        /// </summary>
        /// <remarks>
        /// Searches booths using optional filters.
        /// Supports partial matching for text fields.
        /// Supports Hindi and English names/addresses.
        /// Requires authentication token in header.
        /// </remarks>
        /// <param name="boothNo">Booth number (partial match)</param>
        /// <param name="boothName">Booth name in Hindi or English (partial match)</param>
        /// <param name="boothAddress">Address in Hindi or English (partial match)</param>
        /// <param name="wardNo">Ward number (partial match)</param>
        /// <param name="isMapped">Mapping status: 0=unmapped, 1=mapped, null=all</param>
        /// <returns>Filtered list of booths</returns>
        /// <response code="200">Search completed successfully</response>
        /// <response code="400">Invalid filter parameters</response>
        /// <response code="401">Unauthorized - missing or invalid token</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [Route("booths/search")]
        public HttpResponseMessage SearchBooths(
            [FromUri] string boothNo = null,
            [FromUri] string boothName = null,
            [FromUri] string boothAddress = null,
            [FromUri] string wardNo = null,
            [FromUri] int? isMapped = null)
        {
            var result = _boothService.SearchBooths(boothNo, boothName, boothAddress, wardNo, isMapped);
            
            var statusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
            return Request.CreateResponse(statusCode, result);
        }

        /// <summary>
        /// Update booth GPS location
        /// </summary>
        /// <remarks>
        /// Updates the GPS coordinates of a booth.
        /// Marks booth as "mapped" and records the user who made the update.
        /// Requires authentication token in header.
        /// </remarks>
        /// <param name="id">Booth ID (e.g., BOOTH_000001)</param>
        /// <param name="request">Location update data including latitude, longitude, userId, and optional remarks</param>
        /// <returns>Updated booth data</returns>
        /// <response code="200">Location updated successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="401">Unauthorized - missing or invalid token</response>
        /// <response code="404">Booth not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPut]
        [Route("booths/{id}/location")]
        public HttpResponseMessage UpdateBoothLocation(string id, [FromBody] UpdateBoothLocationRequest request)
        {
            // Validate request
            if (request == null)
            {
                return Request.CreateResponse(
                    HttpStatusCode.BadRequest,
                    BoothApiResponse<object>.CreateError("Request body is required"));
            }

            if (!ModelState.IsValid)
            {
                return Request.CreateResponse(
                    HttpStatusCode.BadRequest,
                    BoothApiResponse<object>.CreateError("Validation failed: " + string.Join(", ", ModelState.Values)));
            }

            // Call service
            var result = _boothService.UpdateBoothLocation(
                id, 
                request.Latitude, 
                request.Longitude, 
                request.UserId, 
                request.Remarks);

            // Determine status code based on message
            HttpStatusCode statusCode;
            if (result.Success)
            {
                statusCode = HttpStatusCode.OK;
            }
            else if (result.Message.Contains("not found"))
            {
                statusCode = HttpStatusCode.NotFound;
            }
            else if (result.Message.Contains("Validation") || result.Message.Contains("must be"))
            {
                statusCode = HttpStatusCode.BadRequest;
            }
            else
            {
                statusCode = HttpStatusCode.InternalServerError;
            }

            return Request.CreateResponse(statusCode, result);
        }
    }
}
