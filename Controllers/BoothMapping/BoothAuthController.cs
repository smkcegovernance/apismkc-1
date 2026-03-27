using System.Net;
using System.Net.Http;
using System.Web.Http;
using SmkcApi.Models.BoothMapping;
using SmkcApi.Services.BoothMapping;

namespace SmkcApi.Controllers.BoothMapping
{
    /// <summary>
    /// Authentication controller for Booth Mapping Application
    /// Uses WEBSITE schema for user authentication
    /// </summary>
    [RoutePrefix("api/booth")]
    public class BoothAuthController : ApiController
    {
        private readonly IBoothAuthService _authService;

        public BoothAuthController(IBoothAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// User login endpoint for booth mapping application
        /// </summary>
        /// <remarks>
        /// Authenticates user with 8-character user ID and password.
        /// Uses WEBSITE schema connection (ws/ws).
        /// Calls SP_USER_LOGIN stored procedure in WEBSITE schema.
        /// Returns user data with token for subsequent API calls.
        /// </remarks>
        /// <param name="request">Login credentials (userId and password)</param>
        /// <returns>Login response with user data and authentication token</returns>
        /// <response code="200">Login successful</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="401">Invalid credentials</response>
        /// <response code="500">Internal server error</response>
        [HttpPost]
        [Route("login")]
        [AllowAnonymous]
        public HttpResponseMessage Login([FromBody] BoothLoginRequest request)
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
            var result = _authService.Login(request.UserId, request.Password);

            // Return appropriate status code
            var statusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.Unauthorized;
            return Request.CreateResponse(statusCode, result);
        }
    }
}
