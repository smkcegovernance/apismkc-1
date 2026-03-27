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
}
