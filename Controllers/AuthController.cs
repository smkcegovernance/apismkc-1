using System.Net;
using System.Net.Http;
using System.Web.Http;
using SmkcApi.Models;
using SmkcApi.Services;

namespace SmkcApi.Controllers
{
    [RoutePrefix("api/auth")]
    public class AuthController : ApiController
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Unified login endpoint for all user types (Bank, Account, Commissioner)
        /// Returns user data with ROLE_ID: 1=Commissioner, 2=Account, 3=Bank
        /// </summary>
        [HttpPost]
        [Route("login")]
        public HttpResponseMessage Login([FromBody] LoginRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.UserId) || string.IsNullOrWhiteSpace(req.Password))
                return Request.CreateResponse(HttpStatusCode.BadRequest, 
                    ApiResponse<object>.CreateError("Invalid request - UserId and Password are required", "VALIDATION"));

            var result = _authService.UnifiedLogin(req.UserId, req.Password);
            return Request.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.Unauthorized, result);
        }

        [HttpGet]
        [Route("profile")]
        public HttpResponseMessage GetProfile([FromUri] string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest,
                    ApiResponse<object>.CreateError("Invalid request - userId is required", "VALIDATION"));
            }

            var result = _authService.GetUserProfile(userId);
            return Request.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest, result);
        }

        [HttpGet]
        [Route("profile/{userId}")]
        public HttpResponseMessage GetProfileByPath(string userId)
        {
            return GetProfile(userId);
        }

        [HttpPost]
        [Route("change-password")]
        public HttpResponseMessage ChangePassword([FromBody] ChangePasswordRequest req)
        {
            if (req == null ||
                string.IsNullOrWhiteSpace(req.UserId) ||
                string.IsNullOrWhiteSpace(req.OldPassword) ||
                string.IsNullOrWhiteSpace(req.NewPassword))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest,
                    ApiResponse<object>.CreateError("Invalid request - UserId, OldPassword and NewPassword are required", "VALIDATION"));
            }

            if (req.NewPassword.Trim().Length < 8)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest,
                    ApiResponse<object>.CreateError("New password must be at least 8 characters", "VALIDATION"));
            }

            var result = _authService.ChangePassword(req.UserId, req.OldPassword, req.NewPassword);
            return Request.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest, result);
        }

        [HttpPost]
        [Route("bank/login")]
        public HttpResponseMessage BankLogin([FromBody] LoginRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.UserId) || string.IsNullOrWhiteSpace(req.Password))
                return Request.CreateResponse(HttpStatusCode.BadRequest, 
                    ApiResponse<object>.CreateError("Invalid request - UserId and Password are required", "VALIDATION"));

            var result = _authService.BankLogin(req.UserId, req.Password);
            return Request.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.Unauthorized, result);
        }

        [HttpPost]
        [Route("account/login")]
        public HttpResponseMessage AccountLogin([FromBody] LoginRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.UserId) || string.IsNullOrWhiteSpace(req.Password))
                return Request.CreateResponse(HttpStatusCode.BadRequest, 
                    ApiResponse<object>.CreateError("Invalid request - UserId and Password are required", "VALIDATION"));

            var result = _authService.AccountLogin(req.UserId, req.Password);
            return Request.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.Unauthorized, result);
        }

        [HttpPost]
        [Route("commissioner/login")]
        public HttpResponseMessage CommissionerLogin([FromBody] LoginRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.UserId) || string.IsNullOrWhiteSpace(req.Password))
                return Request.CreateResponse(HttpStatusCode.BadRequest, 
                    ApiResponse<object>.CreateError("Invalid request - UserId and Password are required", "VALIDATION"));

            var result = _authService.CommissionerLogin(req.UserId, req.Password);
            return Request.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.Unauthorized, result);
        }
    }

    public class LoginRequest
    {
        public string UserId { get; set; }
        public string Password { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string UserId { get; set; }
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
    }
}
