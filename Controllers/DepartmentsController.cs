using System;
using System.Net;
using System.Web.Http;
using SmkcApi.Repositories.Departments;

namespace SmkcApi.Controllers
{
    /// <summary>
    /// Returns the active department configuration list from ERP_DEPT_CONFIG + DEPARTMENTDET.
    /// Used by the frontend to drive the home page tiles and rights admin UI dynamically.
    /// </summary>
    [RoutePrefix("api/departments")]
    public class DepartmentsController : ApiController
    {
        private const int DEFAULT_ULB_CODE = 1;

        private readonly IDepartmentsRepository _repo;

        public DepartmentsController(IDepartmentsRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException("repo");
        }

        // ── GET api/departments/active ────────────────────────────────────────────

        /// <summary>
        /// Returns all active departments with bilingual names and app route keys.
        /// </summary>
        [HttpGet]
        [Route("active")]
        [AllowAnonymous]
        public IHttpActionResult GetActive()
        {
            try
            {
                var data = _repo.GetActiveDepts(DEFAULT_ULB_CODE);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    new { success = false, message = ex.Message });
            }
        }
    }
}
