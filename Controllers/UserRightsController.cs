using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using SmkcApi.Models.UserRights;
using SmkcApi.Repositories.UserRights;

namespace SmkcApi.Controllers
{
    /// <summary>
    /// Manages ERP user menu-access rights.
    /// All write endpoints require the caller to identify themselves as an admin user.
    /// ADMIN001 and PTTEST01 always receive full (IsAdmin=true) rights on read.
    /// </summary>
    [RoutePrefix("api/user-rights")]
    public class UserRightsController : ApiController
    {
        private const int DEFAULT_ULB_CODE = 1;

        private static readonly HashSet<string> AdminUserIds =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ADMIN001", "PTTEST01" };

        private readonly IUserRightsRepository _repo;

        public UserRightsController(IUserRightsRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException("repo");
        }

        // ── GET api/user-rights/for-user?userId=X ────────────────────────────────

        /// <summary>
        /// Returns the effective menu rights for the specified user.
        /// ADMIN001 / PTTEST01 get IsAdmin=true (unlimited access).
        /// </summary>
        [HttpGet]
        [Route("for-user")]
        [AllowAnonymous]
        public IHttpActionResult GetForUser([FromUri] string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Content(HttpStatusCode.BadRequest,
                    new { success = false, message = "userId is required." });

            try
            {
                var data = _repo.GetUserRights(DEFAULT_ULB_CODE, userId.Trim());
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // ── GET api/user-rights/all-users?adminUserId=X ──────────────────────────

        /// <summary>
        /// Lists all active ERP users with dept info and whether they have any rights set.
        /// Requires an admin user ID.
        /// </summary>
        [HttpGet]
        [Route("all-users")]
        [AllowAnonymous]
        public IHttpActionResult GetAllUsers([FromUri] string adminUserId)
        {
            if (!IsAdmin(adminUserId))
                return Content(HttpStatusCode.Forbidden,
                    new { success = false, message = "Admin access required." });

            try
            {
                var data = _repo.GetAllUsers(DEFAULT_ULB_CODE);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // ── POST api/user-rights/save ─────────────────────────────────────────────

        /// <summary>
        /// Replaces all rights for a target user.
        /// Body: { userId, adminUserId, rights: [{deptKey, menuItems:[...]}] }
        /// </summary>
        [HttpPost]
        [Route("save")]
        [AllowAnonymous]
        public IHttpActionResult SaveRights([FromBody] SaveUserRightsRequest req)
        {
            if (req == null)
                return Content(HttpStatusCode.BadRequest,
                    new { success = false, message = "Request body is required." });

            if (!IsAdmin(req.AdminUserId))
                return Content(HttpStatusCode.Forbidden,
                    new { success = false, message = "Admin access required." });

            if (string.IsNullOrWhiteSpace(req.UserId))
                return Content(HttpStatusCode.BadRequest,
                    new { success = false, message = "userId is required." });

            // Prevent overwriting another admin's rights
            if (AdminUserIds.Contains(req.UserId.Trim().ToUpper()))
                return Content(HttpStatusCode.BadRequest,
                    new { success = false, message = "Cannot modify rights for admin users." });

            try
            {
                _repo.SaveUserRights(DEFAULT_ULB_CODE, req.UserId.Trim(),
                    req.Rights ?? new List<DeptRightsItemDto>(),
                    req.AdminUserId.Trim());

                return Ok(new { success = true, message = "Rights saved successfully." });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // ── POST api/user-rights/bulk-seed ───────────────────────────────────────

        /// <summary>
        /// Grants all specified menuItems under deptKey to a list of users.
        /// Existing rights for other departments are preserved.
        /// Body: { adminUserId, deptKey, userIds:[...], menuItems:[...] }
        /// </summary>
        [HttpPost]
        [Route("bulk-seed")]
        [AllowAnonymous]
        public IHttpActionResult BulkSeed([FromBody] BulkSeedRequest req)
        {
            if (req == null)
                return Content(HttpStatusCode.BadRequest,
                    new { success = false, message = "Request body is required." });

            if (!IsAdmin(req.AdminUserId))
                return Content(HttpStatusCode.Forbidden,
                    new { success = false, message = "Admin access required." });

            if (string.IsNullOrWhiteSpace(req.DeptKey))
                return Content(HttpStatusCode.BadRequest,
                    new { success = false, message = "deptKey is required." });

            var userIds   = req.UserIds   ?? new List<string>();
            var menuItems = req.MenuItems ?? new List<string>();

            if (!userIds.Any())
                return Content(HttpStatusCode.BadRequest,
                    new { success = false, message = "At least one userId is required." });
            if (!menuItems.Any())
                return Content(HttpStatusCode.BadRequest,
                    new { success = false, message = "At least one menuItem is required." });

            try
            {
                _repo.BulkSeedRights(DEFAULT_ULB_CODE, req.DeptKey.Trim(),
                    userIds, menuItems, req.AdminUserId.Trim());

                return Ok(new { success = true, message = string.Format(
                    "Granted {0} menu items to {1} users under '{2}'.",
                    menuItems.Count, userIds.Count, req.DeptKey) });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // ── DELETE api/user-rights/clear?userId=X&adminUserId=Y ─────────────────

        /// <summary>
        /// Clears all rights for the specified user (full reset).
        /// </summary>
        [HttpDelete]
        [Route("clear")]
        [AllowAnonymous]
        public IHttpActionResult ClearRights([FromUri] string userId, [FromUri] string adminUserId)
        {
            if (!IsAdmin(adminUserId))
                return Content(HttpStatusCode.Forbidden,
                    new { success = false, message = "Admin access required." });

            if (string.IsNullOrWhiteSpace(userId))
                return Content(HttpStatusCode.BadRequest,
                    new { success = false, message = "userId is required." });

            if (AdminUserIds.Contains(userId.Trim().ToUpper()))
                return Content(HttpStatusCode.BadRequest,
                    new { success = false, message = "Cannot modify rights for admin users." });

            try
            {
                _repo.ClearUserRights(DEFAULT_ULB_CODE, userId.Trim(), adminUserId.Trim());
                return Ok(new { success = true, message = "User rights cleared." });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // ── POST api/user-rights/seed-defaults?adminUserId=X ─────────────────────

        /// <summary>
        /// Seeds default rights for ALL active users:
        ///   Everyone → GAD menus; Accounts dept → Accounts menus; Audit dept → Audit menus.
        /// Detects dept by name — "account"/"लेखा" and "audit"/"लेखापरीक्षण".
        /// Skips rows that already exist.
        /// </summary>
        [HttpPost]
        [Route("seed-defaults")]
        [AllowAnonymous]
        public IHttpActionResult SeedDefaults([FromUri] string adminUserId)
        {
            if (!IsAdmin(adminUserId))
                return Content(HttpStatusCode.Forbidden,
                    new { success = false, message = "Admin access required." });

            try
            {
                var message = _repo.SeedDefaultRights(DEFAULT_ULB_CODE, adminUserId.Trim());
                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static bool IsAdmin(string userId) =>
            !string.IsNullOrWhiteSpace(userId) &&
            AdminUserIds.Contains(userId.Trim().ToUpper());
    }
}
