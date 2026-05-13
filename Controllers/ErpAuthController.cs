using System.Net;
using System.Net.Http;
using System.Web.Http;
using SmkcApi.Models;
using SmkcApi.Services;

namespace SmkcApi.Controllers
{
    /// <summary>
    /// ERP-specific authentication endpoints.
    /// Uses ULBERP.USERDET with BASE64-encoded passwords, validity period,
    /// valid-flag (USER_VIFLAG), lock flag (USER_LOCK), and login history.
    /// Distinct from the deposit-manager AuthController at api/auth/*.
    /// </summary>
    [RoutePrefix("api/erp-auth")]
    public class ErpAuthController : ApiController
    {
        private readonly IErpAuthService _authService;

        public ErpAuthController(IErpAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// POST api/erp-auth/login
        /// Authenticates an ERP user. Locks the account after 3 consecutive failures.
        /// </summary>
        [HttpPost]
        [Route("login")]
        public HttpResponseMessage Login([FromBody] ErpLoginRequest req)
        {
            if (req == null
                || string.IsNullOrWhiteSpace(req.UserId)
                || string.IsNullOrWhiteSpace(req.Password))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest,
                    ApiResponse<object>.CreateError("UserId and Password are required", "VALIDATION"));
            }

            if (req.UserId.Trim().Length > 8 || req.Password.Trim().Length > 8)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest,
                    ApiResponse<object>.CreateError("User ID and password must be maximum 8 characters", "VALIDATION"));
            }

            var ipAddress = GetClientIp();
            var result = _authService.ErpLogin(req.UserId.Trim(), req.Password.Trim(), ipAddress);
            return Request.CreateResponse(
                result.Success ? HttpStatusCode.OK : HttpStatusCode.Unauthorized,
                result);
        }

        /// <summary>
        /// GET api/erp-auth/profile/{userId}
        /// Returns user profile details from ULBERP.USERDET.
        /// </summary>
        [HttpGet]
        [Route("profile/{userId}")]
        public HttpResponseMessage GetProfile(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest,
                    ApiResponse<object>.CreateError("userId is required", "VALIDATION"));
            }

            var result = _authService.ErpGetProfile(userId.Trim());
            return Request.CreateResponse(
                result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest,
                result);
        }

        /// <summary>
        /// POST api/erp-auth/change-password
        /// Changes an ERP user's password. BASE64-encodes the new password before storing.
        /// </summary>
        [HttpPost]
        [Route("change-password")]
        public HttpResponseMessage ChangePassword([FromBody] ErpChangePasswordRequest req)
        {
            if (req == null
                || string.IsNullOrWhiteSpace(req.UserId)
                || string.IsNullOrWhiteSpace(req.OldPassword)
                || string.IsNullOrWhiteSpace(req.NewPassword))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest,
                    ApiResponse<object>.CreateError("UserId, OldPassword and NewPassword are required", "VALIDATION"));
            }

            if (req.NewPassword.Trim().Length > 8)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest,
                    ApiResponse<object>.CreateError("New password must be maximum 8 alphanumeric characters", "VALIDATION"));
            }

            var result = _authService.ErpChangePassword(
                req.UserId.Trim(),
                req.OldPassword.Trim(),
                req.NewPassword.Trim());

            return Request.CreateResponse(
                result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest,
                result);
        }

        /// <summary>
        /// GET api/erp-auth/locked-users?adminUserId=ADMIN001
        /// Returns all user accounts currently locked due to failed login attempts.
        /// Only ADMIN001 / PTTEST01 may call this endpoint.
        /// </summary>
        [HttpGet]
        [Route("locked-users")]
        public HttpResponseMessage GetLockedUsers([FromUri] string adminUserId)
        {
            if (string.IsNullOrWhiteSpace(adminUserId))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest,
                    ApiResponse<object>.CreateError("adminUserId is required", "VALIDATION"));
            }

            var result = _authService.ErpGetLockedUsers(adminUserId.Trim());
            return Request.CreateResponse(
                result.Success ? HttpStatusCode.OK : HttpStatusCode.Forbidden,
                result);
        }

        /// <summary>
        /// POST api/erp-auth/unlock-user
        /// Unlocks a user account locked due to failed login attempts.
        /// Only ADMIN001 / PTTEST01 may call this endpoint.
        /// </summary>
        [HttpPost]
        [Route("unlock-user")]
        public HttpResponseMessage UnlockUser([FromBody] ErpUnlockUserRequest req)
        {
            if (req == null
                || string.IsNullOrWhiteSpace(req.AdminUserId)
                || string.IsNullOrWhiteSpace(req.TargetUserId))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest,
                    ApiResponse<object>.CreateError("AdminUserId and TargetUserId are required", "VALIDATION"));
            }

            var result = _authService.ErpUnlockUser(req.AdminUserId.Trim(), req.TargetUserId.Trim());
            return Request.CreateResponse(
                result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest,
                result);
        }

        private string GetClientIp()
        {
            try
            {
                if (Request.Properties.ContainsKey("MS_HttpContext"))
                {
                    var ctx = Request.Properties["MS_HttpContext"] as System.Web.HttpContextWrapper;
                    return ctx?.Request?.UserHostAddress;
                }
            }
            catch { }
            return null;
        }
    }

    public class ErpLoginRequest
    {
        public string UserId   { get; set; }
        public string Password { get; set; }
    }

    public class ErpChangePasswordRequest
    {
        public string UserId      { get; set; }
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
    }

    public class ErpUnlockUserRequest
    {
        public string AdminUserId  { get; set; }
        public string TargetUserId { get; set; }
    }
}
